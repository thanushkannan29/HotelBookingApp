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
                .Select(h => new
                {
                    h.HotelId,
                    h.Name,
                    h.City,
                    h.ImageUrl,

                    AvgRating = h.Reviews
                        .Select(r => (decimal?)r.Rating)
                        .Average(),

                    ReviewCount = h.Reviews.Count(),

                    StartingPrice = h.RoomTypes!
                        .SelectMany(rt => rt.Rates!)
                        .Select(r => (decimal?)r.Rate)
                        .Min()
                })
                .OrderByDescending(x => x.AvgRating ?? 0)
                .ThenByDescending(x => x.ReviewCount)
                .Take(10)
                .Select(x => new HotelListItemDto
                {
                    HotelId = x.HotelId,
                    Name = x.Name,
                    City = x.City,
                    ImageUrl = x.ImageUrl,

                    AverageRating = Math.Round(x.AvgRating ?? 0m, 2), // ✅ AFTER SQL

                    ReviewCount = x.ReviewCount,

                    StartingPrice = x.StartingPrice ?? 0
                })
                .ToListAsync();
        }

        // ── PUBLIC: SEARCH ────────────────────────────────────────────────────
        public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
        {
            var query = _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin &&
                            h.City.ToLower() == request.City.ToLower());

            // Amenity filter
            if (request.AmenityIds != null && request.AmenityIds.Count > 0)
            {
                query = query.Where(h => h.RoomTypes!.Any(rt =>
                    rt.RoomTypeAmenities!.Any(rta => request.AmenityIds.Contains(rta.AmenityId))));
            }

            // Room type filter
            if (!string.IsNullOrWhiteSpace(request.RoomType))
            {
                query = query.Where(h => h.RoomTypes!.Any(rt =>
                    rt.Name.ToLower().Contains(request.RoomType.ToLower())));
            }

            // Price filter
            if (request.MinPrice.HasValue)
            {
                query = query.Where(h => h.RoomTypes!
                    .SelectMany(rt => rt.Rates!)
                    .Any(r => r.Rate >= request.MinPrice.Value));
            }
            if (request.MaxPrice.HasValue)
            {
                query = query.Where(h => h.RoomTypes!
                    .SelectMany(rt => rt.Rates!)
                    .Any(r => r.Rate <= request.MaxPrice.Value));
            }

            var totalRecords = await query.CountAsync();
            if (totalRecords == 0)
                throw new NotFoundException("No hotels found for the given criteria.");

            // Sort
            IQueryable<Hotel> sorted = request.SortBy switch
            {
                "price_asc" => query.OrderBy(h => h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate) ?? 0),
                "price_desc" => query.OrderByDescending(h => h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate) ?? 0),
                _ => query.OrderBy(h => h.Name)
            };

            var hotels = await sorted
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl,
                    AverageRating = h.Reviews != null && h.Reviews.Any()
                        ? Math.Round((decimal)(h.Reviews.Average(r => (decimal?)r.Rating) ?? 0m), 2) : 0m,
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
                RecordsCount = totalRecords,
                TotalCount = totalRecords
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
            var hotels = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive &&
                            !h.IsBlockedBySuperAdmin &&
                            h.City.ToLower() == city.ToLower())
                .Select(h => new
                {
                    h.HotelId,
                    h.Name,
                    h.City,
                    h.ImageUrl,

                    // ✅ SAFE: EF can translate this
                    AvgRating = h.Reviews.Select(r => (decimal?)r.Rating).Average(),
                    ReviewCount = h.Reviews.Count(),

                    StartingPrice = h.RoomTypes!
                        .SelectMany(rt => rt.Rates!)
                        .Select(r => (decimal?)r.Rate)
                        .Min()
                })
                .ToListAsync();

            // ✅ FINAL MAPPING (IN MEMORY)
            return hotels.Select(h => new HotelListItemDto
            {
                HotelId = h.HotelId,
                Name = h.Name,
                City = h.City,
                ImageUrl = h.ImageUrl,
                AverageRating = Math.Round(h.AvgRating ?? 0m, 2),
                ReviewCount = h.ReviewCount,
                StartingPrice = h.StartingPrice ?? 0
            });
        }


        // ── PUBLIC: HOTEL DETAILS (FULL) ──────────────────────────────────────
        public async Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Include(h => h.RoomTypes!.Where(rt => rt.IsActive))
                    .ThenInclude(rt => rt.RoomTypeAmenities!)
                        .ThenInclude(rta => rta.Amenity)
                .Include(h => h.Reviews!)
                    .ThenInclude(r => r.User)
                .AsSplitQuery()
                .FirstOrDefaultAsync(h => h.HotelId == hotelId)
                ?? throw new NotFoundException("Hotel not found.");

            var reviews = hotel.Reviews ?? new List<Review>();

            // Collect unique amenities from join table (new) + legacy string (fallback)
            var amenitiesFromJoin = hotel.RoomTypes?
                .SelectMany(rt => rt.RoomTypeAmenities ?? Enumerable.Empty<RoomTypeAmenity>())
                .Select(rta => rta.Amenity?.Name ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList() ?? new List<string>();

            var amenitiesFromString = hotel.RoomTypes?
                .Where(rt => !string.IsNullOrEmpty(rt.Amenities))
                .SelectMany(rt => rt.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(a => a.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            var allAmenities = amenitiesFromJoin.Union(amenitiesFromString).Distinct().ToList();

            var roomTypeDtos = hotel.RoomTypes?.Select(t => new RoomTypePublicDto
            {
                RoomTypeId = t.RoomTypeId,
                Name = t.Name,
                Description = t.Description,
                MaxOccupancy = t.MaxOccupancy,
                Amenities = t.RoomTypeAmenities?.Any() == true
                    ? t.RoomTypeAmenities.Select(rta => rta.Amenity?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n))
                    : t.Amenities?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()) ?? Enumerable.Empty<string>(),
                AmenityList = t.RoomTypeAmenities?.Select(rta => new AmenityPublicDto
                {
                    AmenityId = rta.AmenityId,
                    Name = rta.Amenity?.Name ?? string.Empty,
                    Category = rta.Amenity?.Category ?? string.Empty,
                    IconName = rta.Amenity?.IconName
                }) ?? Enumerable.Empty<AmenityPublicDto>(),
                ImageUrl = t.ImageUrl
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
                GstPercent = hotel.GstPercent,
                AverageRating = reviews.Any()
                    ? Math.Round(reviews.Average(r => (decimal)r.Rating), 2) : 0m,
                ReviewCount = reviews.Count,
                Amenities = allAmenities,
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
                .Include(rt => rt.RoomTypeAmenities!)
                    .ThenInclude(rta => rta.Amenity)
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .Select(t => new RoomTypePublicDto
                {
                    RoomTypeId = t.RoomTypeId,
                    Name = t.Name,
                    Description = t.Description,
                    MaxOccupancy = t.MaxOccupancy,
                    Amenities = t.RoomTypeAmenities!.Select(rta => rta.Amenity!.Name),
                    AmenityList = t.RoomTypeAmenities!.Select(rta => new AmenityPublicDto
                    {
                        AmenityId = rta.AmenityId,
                        Name = rta.Amenity!.Name,
                        Category = rta.Amenity.Category,
                        IconName = rta.Amenity.IconName
                    }),
                    ImageUrl = t.ImageUrl
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
                        AvailableRooms = g.Min(x => x.AvailableInventory),
                        ImageUrl = g.Key.ImageUrl
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
                    Before = new { hotel.Name, hotel.Address, hotel.City, hotel.Description, hotel.ContactNumber, hotel.UpiId },
                    After = new { dto.Name, dto.Address, dto.City, dto.Description, dto.ContactNumber, dto.UpiId }
                };

                hotel.Name = dto.Name;
                hotel.Address = dto.Address;
                hotel.City = dto.City;
                hotel.Description = dto.Description;
                hotel.ContactNumber = dto.ContactNumber;
                hotel.ImageUrl = dto.ImageUrl;
                if (dto.UpiId != null) hotel.UpiId = dto.UpiId;

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
        // Fixed: single query with grouped joins instead of N+1 per-hotel loops.
        // Fetches all reservation counts and revenue sums in 2 queries, then merges in memory.
        public async Task<IEnumerable<SuperAdminHotelListDto>> GetAllHotelsForSuperAdminAsync()
        {
            // Query 1: all hotels
            var hotels = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .OrderBy(h => h.Name)
                .ToListAsync();

            // Query 2: reservation counts grouped by hotel
            var reservationCounts = await _reservationRepo.GetQueryable()
                .AsNoTracking()
                .GroupBy(r => r.HotelId)
                .Select(g => new { HotelId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.HotelId, x => x.Count);

            // Query 3: revenue sums grouped by hotel (success transactions only)
            var revenueByHotel = await _transactionRepo.GetQueryable()
                .AsNoTracking()
                .Where(t => t.Status == PaymentStatus.Success)
                .GroupBy(t => t.Reservation!.HotelId)
                .Select(g => new { HotelId = g.Key, Revenue = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(x => x.HotelId, x => x.Revenue);

            // Merge in memory — no N+1
            return hotels.Select(h => new SuperAdminHotelListDto
            {
                HotelId = h.HotelId,
                Name = h.Name,
                City = h.City,
                ContactNumber = h.ContactNumber,
                IsActive = h.IsActive,
                IsBlockedBySuperAdmin = h.IsBlockedBySuperAdmin,
                CreatedAt = h.CreatedAt,
                TotalReservations = reservationCounts.TryGetValue(h.HotelId, out var rc) ? rc : 0,
                TotalRevenue = revenueByHotel.TryGetValue(h.HotelId, out var rv) ? rv : 0m
            });
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

        // ── ADMIN: UPDATE GST ─────────────────────────────────────────────────
        public async Task UpdateHotelGstAsync(Guid adminUserId, decimal gstPercent)
        {
            var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (user.HotelId == null)
                throw new UnAuthorizedException("No hotel associated with this admin.");

            var hotel = await _hotelRepo.GetAsync(user.HotelId.Value)
                ?? throw new NotFoundException("Hotel not found.");

            hotel.GstPercent = gstPercent;
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(adminUserId, "HotelGstUpdated", "Hotel",
                hotel.HotelId, $"GST set to {gstPercent}%");
        }

        // ── SUPERADMIN: LIST ALL HOTELS (paged) ───────────────────────────────
        public async Task<PagedSuperAdminHotelResponseDto> GetAllHotelsForSuperAdminPagedAsync(int page, int pageSize)
        {
            var totalCount = await _hotelRepo.GetQueryable().CountAsync();

            var hotels = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .OrderBy(h => h.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var hotelIds = hotels.Select(h => h.HotelId).ToList();

            var reservationCounts = await _reservationRepo.GetQueryable()
                .AsNoTracking()
                .Where(r => hotelIds.Contains(r.HotelId))
                .GroupBy(r => r.HotelId)
                .Select(g => new { HotelId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.HotelId, x => x.Count);

            var revenueByHotel = await _transactionRepo.GetQueryable()
                .AsNoTracking()
                .Where(t => t.Status == PaymentStatus.Success && hotelIds.Contains(t.Reservation!.HotelId))
                .GroupBy(t => t.Reservation!.HotelId)
                .Select(g => new { HotelId = g.Key, Revenue = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(x => x.HotelId, x => x.Revenue);

            var dtos = hotels.Select(h => new SuperAdminHotelListDto
            {
                HotelId = h.HotelId,
                Name = h.Name,
                City = h.City,
                ContactNumber = h.ContactNumber,
                IsActive = h.IsActive,
                IsBlockedBySuperAdmin = h.IsBlockedBySuperAdmin,
                CreatedAt = h.CreatedAt,
                TotalReservations = reservationCounts.TryGetValue(h.HotelId, out var rc) ? rc : 0,
                TotalRevenue = revenueByHotel.TryGetValue(h.HotelId, out var rev) ? rev : 0
            });

            return new PagedSuperAdminHotelResponseDto { TotalCount = totalCount, Hotels = dtos };
        }
    }
}