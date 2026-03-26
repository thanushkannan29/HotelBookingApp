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

        // ── ADD ROOM TYPE ─────────────────────────────────────────────────────
        public async Task AddRoomTypeAsync(Guid userId, CreateRoomTypeDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                if (user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized.");

                var roomType = new RoomType
                {
                    RoomTypeId = Guid.NewGuid(),
                    HotelId = user.HotelId.Value,
                    Name = dto.Name,
                    Description = dto.Description,
                    MaxOccupancy = dto.MaxOccupancy,
                    Amenities = string.Empty,
                    ImageUrl = dto.ImageUrl,
                    IsActive = true
                };

                await _roomTypeRepo.AddAsync(roomType);
                await _unitOfWork.CommitAsync();

                // Save amenity associations
                if (dto.AmenityIds != null && dto.AmenityIds.Count > 0)
                {
                    foreach (var amenityId in dto.AmenityIds)
                    {
                        await _roomTypeAmenityRepo.AddAsync(new RoomTypeAmenity
                        {
                            RoomTypeId = roomType.RoomTypeId,
                            AmenityId = amenityId
                        });
                    }
                    await _unitOfWork.CommitAsync();
                }

                await _auditLogService.LogAsync(userId, "RoomTypeAdded", "RoomType",
                    roomType.RoomTypeId, JsonSerializer.Serialize(dto));
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── UPDATE ROOM TYPE ──────────────────────────────────────────────────
        public async Task UpdateRoomTypeAsync(Guid userId, UpdateRoomTypeDto dto)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            var roomType = await _roomTypeRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.RoomTypeId == dto.RoomTypeId && r.HotelId == user.HotelId)
                ?? throw new NotFoundException("RoomType not found.");

            var before = new { roomType.Name, roomType.Description, roomType.MaxOccupancy, roomType.Amenities, roomType.ImageUrl };

            roomType.Name = dto.Name;
            roomType.Description = dto.Description;
            roomType.MaxOccupancy = dto.MaxOccupancy;
            roomType.ImageUrl = dto.ImageUrl;

            // Update amenity associations
            if (dto.AmenityIds != null)
            {
                var existing = await _context.RoomTypeAmenities
                    .Where(rta => rta.RoomTypeId == dto.RoomTypeId)
                    .ToListAsync();

                _context.RoomTypeAmenities.RemoveRange(existing);

                foreach (var amenityId in dto.AmenityIds)
                {
                    await _roomTypeAmenityRepo.AddAsync(new RoomTypeAmenity
                    {
                        RoomTypeId = dto.RoomTypeId,
                        AmenityId = amenityId
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, "RoomTypeUpdated", "RoomType",
                roomType.RoomTypeId, JsonSerializer.Serialize(new { Before = before, After = dto }));
        }

        // ── TOGGLE ROOM TYPE STATUS ───────────────────────────────────────────
        public async Task ToggleRoomTypeStatusAsync(Guid userId, Guid roomTypeId, bool isActive)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            var roomType = await _roomTypeRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.RoomTypeId == roomTypeId && r.HotelId == user.HotelId)
                ?? throw new NotFoundException("RoomType not found.");

            roomType.IsActive = isActive;
            await _unitOfWork.SaveChangesAsync();
        }

        // ── ADD RATE ──────────────────────────────────────────────────────────
        public async Task AddRateAsync(Guid userId, CreateRoomTypeRateDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                var roomTypeExists = await _roomTypeRepo.GetQueryable()
                    .AnyAsync(r => r.RoomTypeId == dto.RoomTypeId && r.HotelId == user.HotelId);

                if (!roomTypeExists)
                    throw new NotFoundException("RoomType not found.");

                if (dto.StartDate > dto.EndDate)
                    throw new ValidationException("Start date must be before end date.");

                var overlapping = await _rateRepo.GetQueryable()
                    .AnyAsync(r => r.RoomTypeId == dto.RoomTypeId &&
                                   dto.StartDate <= r.EndDate &&
                                   dto.EndDate >= r.StartDate);

                if (overlapping)
                    throw new ConflictException("Rate already exists for this date range.");

                var rate = new RoomTypeRate
                {
                    RoomTypeRateId = Guid.NewGuid(),
                    RoomTypeId = dto.RoomTypeId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Rate = dto.Rate
                };

                await _rateRepo.AddAsync(rate);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── UPDATE RATE ───────────────────────────────────────────────────────
        public async Task UpdateRateAsync(Guid userId, UpdateRoomTypeRateDto dto)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            var rate = await _rateRepo.GetQueryable()
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomTypeRateId == dto.RoomTypeRateId);

            if (rate == null || rate.RoomType!.HotelId != user.HotelId)
                throw new UnAuthorizedException("Unauthorized.");

            rate.StartDate = dto.StartDate;
            rate.EndDate = dto.EndDate;
            rate.Rate = dto.Rate;

            await _unitOfWork.SaveChangesAsync();
        }

        // ── GET RATE BY DATE ──────────────────────────────────────────────────
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

        // ── GET ROOM TYPES BY HOTEL ───────────────────────────────────────────
        public async Task<IEnumerable<RoomTypeListDto>> GetRoomTypesByHotelAsync(Guid userId)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized.");

            return await _roomTypeRepo.GetQueryable()
                .Include(rt => rt.RoomTypeAmenities!)
                    .ThenInclude(rta => rta.Amenity)
                .Where(rt => rt.HotelId == user.HotelId)
                .Select(rt => new RoomTypeListDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    Name = rt.Name,
                    Description = rt.Description,
                    MaxOccupancy = rt.MaxOccupancy,
                    Amenities = rt.Amenities,
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
                })
                .ToListAsync();
        }

        // ── GET ROOM TYPES BY HOTEL (paged) ───────────────────────────────────
        public async Task<PagedRoomTypeResponseDto> GetRoomTypesByHotelPagedAsync(Guid userId, int page, int pageSize)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized.");

            var query = _roomTypeRepo.GetQueryable()
                .Include(rt => rt.RoomTypeAmenities!)
                    .ThenInclude(rta => rta.Amenity)
                .Where(rt => rt.HotelId == user.HotelId);

            var total = await query.CountAsync();
            var roomTypes = await query
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(rt => new RoomTypeListDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    Name = rt.Name,
                    Description = rt.Description,
                    MaxOccupancy = rt.MaxOccupancy,
                    Amenities = rt.Amenities,
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
                })
                .ToListAsync();

            return new PagedRoomTypeResponseDto { TotalCount = total, RoomTypes = roomTypes };
        }

        // ── GET RATES FOR ROOM TYPE ───────────────────────────────────────────
        public async Task<IEnumerable<RoomTypeRateDto>> GetRatesAsync(Guid userId, Guid roomTypeId)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized.");

            var rates = await _rateRepo.GetQueryable()
                .Where(r => r.RoomTypeId == roomTypeId)
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            return rates.Select(r => new RoomTypeRateDto
            {
                RoomTypeRateId = r.RoomTypeRateId,
                RoomTypeId = r.RoomTypeId,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                Rate = r.Rate
            });
        }
    }
}