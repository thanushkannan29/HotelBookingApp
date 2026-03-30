using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages room types, their amenity associations, and date-based pricing rates.
    /// Amenity updates use a remove-and-reinsert strategy within a single transaction.
    /// </summary>
    public class RoomTypeService : IRoomTypeService
    {
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, RoomTypeRate> _rateRepo;
        private readonly IRepository<Guid, RoomTypeAmenity> _roomTypeAmenityRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly HotelBookingContext _context;

        public RoomTypeService(
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeRate> rateRepo,
            IRepository<Guid, RoomTypeAmenity> roomTypeAmenityRepo,
            IRepository<Guid, User> userRepo,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork,
            HotelBookingContext context)
        {
            _roomTypeRepo = roomTypeRepo;
            _rateRepo = rateRepo;
            _roomTypeAmenityRepo = roomTypeAmenityRepo;
            _userRepo = userRepo;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task AddRoomTypeAsync(Guid userId, CreateRoomTypeDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var hotelId = await GetAdminHotelIdOrThrowAsync(userId);
                var roomType = BuildRoomType(dto, hotelId);
                await _roomTypeRepo.AddAsync(roomType);
                await SaveAmenityAssociationsAsync(roomType.RoomTypeId, dto.AmenityIds);
                await _unitOfWork.CommitAsync();
                await _auditLogService.LogAsync(userId, "RoomTypeAdded", "RoomType",
                    roomType.RoomTypeId, JsonSerializer.Serialize(dto));
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateRoomTypeAsync(Guid userId, UpdateRoomTypeDto dto)
        {
            var user = await GetUserOrThrowAsync(userId);
            var roomType = await GetRoomTypeForHotelOrThrowAsync(dto.RoomTypeId, user.HotelId);
            var before = new { roomType.Name, roomType.Description, roomType.MaxOccupancy, roomType.ImageUrl };

            ApplyRoomTypeUpdates(roomType, dto);
            if (dto.AmenityIds is not null)
                await ReplaceAmenityAssociationsAsync(dto.RoomTypeId, dto.AmenityIds);

            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, "RoomTypeUpdated", "RoomType",
                roomType.RoomTypeId, JsonSerializer.Serialize(new { Before = before, After = dto }));
        }

        public async Task ToggleRoomTypeStatusAsync(Guid userId, Guid roomTypeId, bool isActive)
        {
            var user = await GetUserOrThrowAsync(userId);
            var roomType = await GetRoomTypeForHotelOrThrowAsync(roomTypeId, user.HotelId);
            roomType.IsActive = isActive;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task AddRateAsync(Guid userId, CreateRoomTypeRateDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await ValidateRateOwnershipAsync(userId, dto.RoomTypeId);
                ValidateDateRange(dto.StartDate, dto.EndDate);
                await EnsureNoOverlappingRateAsync(dto.RoomTypeId, dto.StartDate, dto.EndDate);

                await _rateRepo.AddAsync(BuildRate(dto));
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateRateAsync(Guid userId, UpdateRoomTypeRateDto dto)
        {
            var user = await GetUserOrThrowAsync(userId);
            var rate = await GetRateForHotelOrThrowAsync(dto.RoomTypeRateId, user.HotelId);
            rate.StartDate = dto.StartDate;
            rate.EndDate = dto.EndDate;
            rate.Rate = dto.Rate;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<decimal> GetRateByDateAsync(Guid userId, GetRateByDateRequestDto dto)
        {
            var rate = await _rateRepo.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    dto.Date >= r.StartDate &&
                    dto.Date <= r.EndDate)
                ?? throw new NotFoundException("Rate not found for the given date.");
            return rate.Rate;
        }

        public async Task<IEnumerable<RoomTypeListDto>> GetRoomTypesByHotelAsync(Guid userId)
        {
            var hotelId = await GetAdminHotelIdOrThrowAsync(userId);
            return await BuildRoomTypeQuery(hotelId).ToListAsync();
        }

        public async Task<PagedRoomTypeResponseDto> GetRoomTypesByHotelPagedAsync(
            Guid userId, int page, int pageSize)
        {
            var hotelId = await GetAdminHotelIdOrThrowAsync(userId);
            var query = BuildRoomTypeQuery(hotelId);
            var total = await _roomTypeRepo.GetQueryable()
                .CountAsync(rt => rt.HotelId == hotelId);
            var roomTypes = await query
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();
            return new PagedRoomTypeResponseDto { TotalCount = total, RoomTypes = roomTypes };
        }

        public async Task<IEnumerable<RoomTypeRateDto>> GetRatesAsync(Guid userId, Guid roomTypeId)
        {
            await GetAdminHotelIdOrThrowAsync(userId); // authorization check
            var rates = await _rateRepo.GetQueryable()
                .Where(r => r.RoomTypeId == roomTypeId)
                .OrderBy(r => r.StartDate)
                .ToListAsync();
            return rates.Select(MapRateToDto);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<Guid> GetAdminHotelIdOrThrowAsync(Guid userId)
        {
            var user = await GetUserOrThrowAsync(userId);
            if (user.HotelId is null) throw new UnAuthorizedException("Unauthorized.");
            return user.HotelId.Value;
        }

        private async Task<User> GetUserOrThrowAsync(Guid userId)
            => await _userRepo.GetAsync(userId) ?? throw new UnAuthorizedException("Unauthorized.");

        private async Task<RoomType> GetRoomTypeForHotelOrThrowAsync(Guid roomTypeId, Guid? hotelId)
            => await _roomTypeRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.RoomTypeId == roomTypeId && r.HotelId == hotelId)
                ?? throw new NotFoundException("RoomType not found.");

        private async Task<RoomTypeRate> GetRateForHotelOrThrowAsync(Guid rateId, Guid? hotelId)
        {
            var rate = await _rateRepo.GetQueryable()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomTypeRateId == rateId);
            if (rate is null || rate.RoomType!.HotelId != hotelId)
                throw new UnAuthorizedException("Unauthorized.");
            return rate;
        }

        private async Task ValidateRateOwnershipAsync(Guid userId, Guid roomTypeId)
        {
            var user = await GetUserOrThrowAsync(userId);
            var exists = await _roomTypeRepo.GetQueryable()
                .AnyAsync(r => r.RoomTypeId == roomTypeId && r.HotelId == user.HotelId);
            if (!exists) throw new NotFoundException("RoomType not found.");
        }

        private static void ValidateDateRange(DateOnly start, DateOnly end)
        {
            if (start > end) throw new ValidationException("Start date must be before end date.");
        }

        private async Task EnsureNoOverlappingRateAsync(
            Guid roomTypeId, DateOnly start, DateOnly end)
        {
            var overlapping = await _rateRepo.GetQueryable()
                .AnyAsync(r => r.RoomTypeId == roomTypeId &&
                               start <= r.EndDate && end >= r.StartDate);
            if (overlapping) throw new ConflictException("Rate already exists for this date range.");
        }

        private async Task SaveAmenityAssociationsAsync(
            Guid roomTypeId, List<Guid>? amenityIds)
        {
            if (amenityIds is null || amenityIds.Count == 0) return;
            foreach (var amenityId in amenityIds)
                await _roomTypeAmenityRepo.AddAsync(new RoomTypeAmenity
                {
                    RoomTypeId = roomTypeId,
                    AmenityId = amenityId
                });
        }

        private async Task ReplaceAmenityAssociationsAsync(
            Guid roomTypeId, List<Guid> amenityIds)
        {
            var existing = await _context.RoomTypeAmenities
                .Where(rta => rta.RoomTypeId == roomTypeId)
                .ToListAsync();
            _context.RoomTypeAmenities.RemoveRange(existing);
            await SaveAmenityAssociationsAsync(roomTypeId, amenityIds);
        }

        private static RoomType BuildRoomType(CreateRoomTypeDto dto, Guid hotelId) => new()
        {
            RoomTypeId = Guid.NewGuid(),
            HotelId = hotelId,
            Name = dto.Name,
            Description = dto.Description,
            MaxOccupancy = dto.MaxOccupancy,
            ImageUrl = dto.ImageUrl,
            IsActive = true
        };

        private static void ApplyRoomTypeUpdates(RoomType roomType, UpdateRoomTypeDto dto)
        {
            roomType.Name = dto.Name;
            roomType.Description = dto.Description;
            roomType.MaxOccupancy = dto.MaxOccupancy;
            roomType.ImageUrl = dto.ImageUrl;
        }

        private static RoomTypeRate BuildRate(CreateRoomTypeRateDto dto) => new()
        {
            RoomTypeRateId = Guid.NewGuid(),
            RoomTypeId = dto.RoomTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Rate = dto.Rate
        };

        private IQueryable<RoomTypeListDto> BuildRoomTypeQuery(Guid hotelId)
            => _roomTypeRepo.GetQueryable()
                .Include(rt => rt.RoomTypeAmenities!).ThenInclude(rta => rta.Amenity)
                .Where(rt => rt.HotelId == hotelId)
                .Select(rt => new RoomTypeListDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    Name = rt.Name,
                    Description = rt.Description,
                    MaxOccupancy = rt.MaxOccupancy,
                    AmenityList = rt.RoomTypeAmenities!.Select(rta => new AmenityItemDto
                    {
                        AmenityId = rta.AmenityId,
                        Name = rta.Amenity!.Name,
                        Category = rta.Amenity.Category,
                        IconName = rta.Amenity.IconName
                    }).ToList(),
                    IsActive = rt.IsActive,
                    RoomCount = rt.Rooms!.Count,
                    ImageUrl = rt.ImageUrl
                });

        private static RoomTypeRateDto MapRateToDto(RoomTypeRate rate) => new()
        {
            RoomTypeRateId = rate.RoomTypeRateId,
            RoomTypeId = rate.RoomTypeId,
            StartDate = rate.StartDate,
            EndDate = rate.EndDate,
            Rate = rate.Rate
        };
    }
}
