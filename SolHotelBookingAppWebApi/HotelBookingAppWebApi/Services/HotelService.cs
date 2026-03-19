using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.DTOs.Hotel.SuperAdmin;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelBookingAppWebApi.Services
{
    public class HotelService : IHotelService
    {
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, Transaction> _transactionRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        public HotelService(
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, User> userRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, Transaction> transactionRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork)
        {
            _hotelRepo = hotelRepo;
            _userRepo = userRepo;
            _roomTypeRepo = roomTypeRepo;
            _transactionRepo = transactionRepo;
            _reservationRepo = reservationRepo;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC: TOP HOTELS ────────────────────────────────────────────────
        public async Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync()
        {
            return await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin)
                .Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.Reviews!.Any()
                        ? Math.Round(h.Reviews.Average(r => (decimal)r.Rating), 2) : 0m,
                    ReviewCount = h.Reviews!.Count(),
                    StartingPrice = h.RoomTypes!
                        .SelectMany(rt => rt.Rates!)
                        .Min(r => (decimal?)r.Rate) ?? 0
                })
                .OrderByDescending(h => h.AverageRating)
                .ThenByDescending(h => h.ReviewCount)
                .Take(10)
                .ToListAsync();
        }

        // ── PUBLIC: SEARCH ────────────────────────────────────────────────────
        public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
        {
            var query = _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin &&
                            h.City.ToLower() == request.City.ToLower());

            var totalRecords = await query.CountAsync();
            if (totalRecords == 0)
                throw new NotFoundException("No hotels found for the given city.");

            var hotels = await query
                .OrderBy(h => h.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.Reviews!.Any()
                        ? Math.Round(h.Reviews.Average(r => (decimal)r.Rating), 2) : 0m,
                    ReviewCount = h.Reviews!.Count(),
                    StartingPrice = h.RoomTypes!
                        .SelectMany(rt => rt.Rates!)
                        .Min(r => (decimal?)r.Rate) ?? 0
                })
                .ToListAsync();

            return new SearchHotelResponseDto
            {
                Hotels = hotels,
                PageNumber = request.PageNumber,
                RecordsCount = totalRecords
            };
        }

        // ── PUBLIC: CITIES ────────────────────────────────────────────────────
        public async Task<IEnumerable<string>> GetCitiesAsync()
        {
            return await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin)
                .Select(h => h.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        // ── PUBLIC: HOTELS BY CITY ────────────────────────────────────────────
        public async Task<IEnumerable<HotelListItemDto>> GetHotelsByCityAsync(string city)
        {
            return await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin &&
                            h.City.ToLower() == city.ToLower())
                .Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.Reviews!.Any()
                        ? Math.Round(h.Reviews.Average(r => (decimal)r.Rating), 2) : 0m,
                    ReviewCount = h.Reviews!.Count(),
                    StartingPrice = h.RoomTypes!
                        .SelectMany(rt => rt.Rates!)
                        .Min(r => (decimal?)r.Rate) ?? 0
                })
                .OrderByDescending(h => h.AverageRating)
                .ToListAsync();
        }

        // ── PUBLIC: HOTEL DETAILS (FULL) ──────────────────────────────────────
        public async Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Include(h => h.RoomTypes!.Where(rt => rt.IsActive))
                .Include(h => h.Reviews!)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(h => h.HotelId == hotelId)
                ?? throw new NotFoundException("Hotel not found.");

            var reviews = hotel.Reviews ?? new List<Review>();
            var amenities = hotel.RoomTypes?
                .Where(rt => !string.IsNullOrEmpty(rt.Amenities))
                .SelectMany(rt => rt.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(a => a.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            var roomTypeDtos = hotel.RoomTypes?.Select(t => new RoomTypePublicDto
            {
                RoomTypeId = t.RoomTypeId,
                Name = t.Name,
                Description = t.Description,
                MaxOccupancy = t.MaxOccupancy,
                Amenities = t.Amenities?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim()) ?? Enumerable.Empty<string>()
            }) ?? Enumerable.Empty<RoomTypePublicDto>();

            return new HotelDetailsDto
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Address = hotel.Address,
                City = hotel.City,
                Description = hotel.Description,
                ImageUrl = hotel.ImageUrl,
                ContactNumber = hotel.ContactNumber,
                AverageRating = reviews.Any()
                    ? Math.Round(reviews.Average(r => (decimal)r.Rating), 2) : 0m,
                ReviewCount = reviews.Count,
                Amenities = amenities,
                RoomTypes = roomTypeDtos,
                Reviews = reviews.OrderByDescending(r => r.CreatedDate).Take(10).Select(r => new ReviewDto
                {
                    UserName = r.User?.Name ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ImageUrl = r.ImageUrl,
                    CreatedDate = r.CreatedDate
                })
            };
        }

        // ── PUBLIC: ROOM TYPES ────────────────────────────────────────────────
        public async Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId)
        {
            return await _roomTypeRepo.GetQueryable()
                .AsNoTracking()
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .Select(t => new RoomTypePublicDto
                {
                    RoomTypeId = t.RoomTypeId,
                    Name = t.Name,
                    Description = t.Description,
                    MaxOccupancy = t.MaxOccupancy,
                    Amenities = t.Amenities != null
                        ? t.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries) : new string[] { }
                })
                .ToListAsync();
        }

        // ── PUBLIC: AVAILABILITY ──────────────────────────────────────────────
        public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(
            Guid hotelId, DateOnly checkIn, DateOnly checkOut)
        {
            var inventories = await _roomTypeRepo.GetQueryable()
                .AsNoTracking()
                .Where(rt => rt.HotelId == hotelId && rt.IsActive)
                .SelectMany(rt => rt.Inventories!)
                .Where(i => i.Date >= checkIn && i.Date < checkOut)
                .Include(i => i.RoomType!)
                    .ThenInclude(rt => rt.Rates)
                .ToListAsync();

            return inventories
                .GroupBy(i => i.RoomType!)
                .Select(g =>
                {
                    var rate = g.Key.Rates?
                        .FirstOrDefault(r => checkIn >= r.StartDate && checkIn <= r.EndDate);
                    return new RoomAvailabilityDto
                    {
                        RoomTypeId = g.Key.RoomTypeId,
                        RoomTypeName = g.Key.Name,
                        PricePerNight = rate?.Rate ?? 0,
                        AvailableRooms = g.Min(x => x.AvailableInventory)
                    };
                });
        }

        // ── ADMIN: UPDATE HOTEL ───────────────────────────────────────────────
        public async Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                if (user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized.");

                var hotel = await _hotelRepo.GetAsync(user.HotelId.Value)
                    ?? throw new NotFoundException("Hotel not found.");

                var changes = new
                {
                    Before = new { hotel.Name, hotel.Address, hotel.City, hotel.Description, hotel.ContactNumber },
                    After = new { dto.Name, dto.Address, dto.City, dto.Description, dto.ContactNumber }
                };

                hotel.Name = dto.Name;
                hotel.Address = dto.Address;
                hotel.City = dto.City;
                hotel.Description = dto.Description;
                hotel.ContactNumber = dto.ContactNumber;
                hotel.ImageUrl = dto.ImageUrl;

                await _unitOfWork.CommitAsync();

                await _auditLogService.LogAsync(userId, "HotelUpdated", "Hotel",
                    hotel.HotelId, JsonSerializer.Serialize(changes));
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── ADMIN: TOGGLE HOTEL STATUS ────────────────────────────────────────
        public async Task ToggleHotelStatusAsync(Guid userId, bool isActive)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                if (user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized.");

                var hotel = await _hotelRepo.GetAsync(user.HotelId.Value)
                    ?? throw new NotFoundException("Hotel not found.");

                // Admin cannot activate a hotel blocked by SuperAdmin
                if (isActive && hotel.IsBlockedBySuperAdmin)
                    throw new ValidationException("Hotel is blocked by SuperAdmin and cannot be activated.");

                hotel.IsActive = isActive;
                await _unitOfWork.CommitAsync();

                await _auditLogService.LogAsync(userId, isActive ? "HotelActivated" : "HotelDeactivated",
                    "Hotel", hotel.HotelId, $"IsActive set to {isActive}");
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── SUPERADMIN: LIST ALL HOTELS ───────────────────────────────────────
        public async Task<IEnumerable<SuperAdminHotelListDto>> GetAllHotelsForSuperAdminAsync()
        {
            var hotels = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .ToListAsync();

            var result = new List<SuperAdminHotelListDto>();

            foreach (var h in hotels)
            {
                var totalRes = await _reservationRepo.GetQueryable()
                    .CountAsync(r => r.HotelId == h.HotelId);

                var totalRev = await _transactionRepo.GetQueryable()
                    .Where(t => t.Status == PaymentStatus.Success && t.Reservation!.HotelId == h.HotelId)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                result.Add(new SuperAdminHotelListDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ContactNumber = h.ContactNumber,
                    IsActive = h.IsActive,
                    IsBlockedBySuperAdmin = h.IsBlockedBySuperAdmin,
                    CreatedAt = h.CreatedAt,
                    TotalReservations = totalRes,
                    TotalRevenue = totalRev
                });
            }

            return result;
        }

        // ── SUPERADMIN: BLOCK HOTEL ───────────────────────────────────────────
        public async Task BlockHotelAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetAsync(hotelId)
                ?? throw new NotFoundException("Hotel not found.");

            hotel.IsBlockedBySuperAdmin = true;
            hotel.IsActive = false;

            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync(null, "HotelBlocked", "Hotel", hotelId,
                "Hotel blocked by SuperAdmin.");
        }

        // ── SUPERADMIN: UNBLOCK HOTEL ─────────────────────────────────────────
        public async Task UnblockHotelAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetAsync(hotelId)
                ?? throw new NotFoundException("Hotel not found.");

            hotel.IsBlockedBySuperAdmin = false;

            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync(null, "HotelUnblocked", "Hotel", hotelId,
                "Hotel unblocked by SuperAdmin.");
        }
    }
}
