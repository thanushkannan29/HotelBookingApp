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
    /// <summary>
    /// Manages hotel data for public browsing, admin self-management, and SuperAdmin oversight.
    /// Public queries use AsNoTracking for performance; write operations use transactions.
    /// </summary>
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
            var raw = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin)
                .Select(h => new
                {
                    h.HotelId, h.Name, h.City, h.ImageUrl,
                    AvgRating = h.Reviews!.Select(r => (decimal?)r.Rating).Average(),
                    ReviewCount = h.Reviews!.Count(),
                    StartingPrice = h.RoomTypes!.SelectMany(rt => rt.Rates!).Select(r => (decimal?)r.Rate).Min()
                })
                .OrderByDescending(x => x.AvgRating ?? 0)
                .ThenByDescending(x => x.ReviewCount)
                .Take(10)
                .ToListAsync();

            return raw.Select(x => MapToListItemDto(
                x.HotelId, x.Name, x.City, x.ImageUrl,
                x.AvgRating, x.ReviewCount, x.StartingPrice));
        }

        // ── PUBLIC: SEARCH ────────────────────────────────────────────────────

        public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
        {
            var query = BuildPublicHotelQuery();
            query = ApplySearchFilters(query, request);

            var totalRecords = await query.CountAsync();
            if (totalRecords == 0)
                return EmptySearchResponse(request.PageNumber);

            var sorted = ApplySorting(query, request.SortBy);
            var hotels = await sorted
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId, Name = h.Name, City = h.City, ImageUrl = h.ImageUrl,
                    AverageRating = h.Reviews != null && h.Reviews.Any()
                        ? Math.Round((decimal)(h.Reviews.Average(r => (decimal?)r.Rating) ?? 0m), 2) : 0m,
                    ReviewCount = h.Reviews!.Count(),
                    StartingPrice = h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate) ?? 0
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

        // ── PUBLIC: CITIES / STATES ───────────────────────────────────────────

        public async Task<IEnumerable<string>> GetCitiesAsync()
            => await _hotelRepo.GetQueryable().AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin)
                .Select(h => h.City).Distinct().OrderBy(c => c).ToListAsync();

        public async Task<IEnumerable<string>> GetActiveStatesAsync()
            => await _hotelRepo.GetQueryable().AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin && !string.IsNullOrEmpty(h.State))
                .Select(h => h.State).Distinct().OrderBy(s => s).ToListAsync();

        // ── PUBLIC: HOTELS BY CITY / STATE ────────────────────────────────────

        public async Task<IEnumerable<HotelListItemDto>> GetHotelsByCityAsync(string city)
            => await FetchHotelListAsync(h => h.City.ToLower() == city.ToLower());

        public async Task<IEnumerable<HotelListItemDto>> GetHotelsByStateAsync(string stateName)
            => await FetchHotelListAsync(h => h.State.ToLower() == stateName.ToLower(), take: 10);

        // ── PUBLIC: HOTEL DETAILS ─────────────────────────────────────────────

        public async Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetQueryable()
                .AsNoTracking()
                .Include(h => h.RoomTypes!.Where(rt => rt.IsActive))
                    .ThenInclude(rt => rt.RoomTypeAmenities!).ThenInclude(rta => rta.Amenity)
                .Include(h => h.Reviews!).ThenInclude(r => r.User!).ThenInclude(u => u.UserDetails)
                .AsSplitQuery()
                .FirstOrDefaultAsync(h => h.HotelId == hotelId)
                ?? throw new NotFoundException("Hotel not found.");

            return BuildHotelDetailsDto(hotel);
        }

        // ── PUBLIC: ROOM TYPES ────────────────────────────────────────────────

        public async Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId)
            => await _roomTypeRepo.GetQueryable()
                .AsNoTracking()
                .Include(rt => rt.RoomTypeAmenities!).ThenInclude(rta => rta.Amenity)
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .Select(t => MapToRoomTypePublicDto(t))
                .ToListAsync();

        // ── PUBLIC: AVAILABILITY ──────────────────────────────────────────────

        public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(
            Guid hotelId, DateOnly checkIn, DateOnly checkOut)
        {
            var inventories = await _roomTypeRepo.GetQueryable()
                .AsNoTracking()
                .Where(rt => rt.HotelId == hotelId && rt.IsActive)
                .SelectMany(rt => rt.Inventories!)
                .Where(i => i.Date >= checkIn && i.Date < checkOut)
                .Include(i => i.RoomType!).ThenInclude(rt => rt.Rates)
                .ToListAsync();

            return inventories.GroupBy(i => i.RoomType!).Select(g =>
            {
                var rate = g.Key.Rates?.FirstOrDefault(r => checkIn >= r.StartDate && checkIn <= r.EndDate);
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
                var (user, hotel) = await GetAdminWithHotelAsync(userId);
                var changes = new
                {
                    Before = new { hotel.Name, hotel.Address, hotel.City, hotel.Description, hotel.ContactNumber, hotel.UpiId },
                    After = new { dto.Name, dto.Address, dto.City, dto.Description, dto.ContactNumber, dto.UpiId }
                };
                ApplyHotelUpdates(hotel, dto);
                await _unitOfWork.CommitAsync();
                await _auditLogService.LogAsync(userId, "HotelUpdated", "Hotel",
                    hotel.HotelId, JsonSerializer.Serialize(changes));
            }
            catch { await _unitOfWork.RollbackAsync(); throw; }
        }

        // ── ADMIN: TOGGLE STATUS ──────────────────────────────────────────────

        public async Task ToggleHotelStatusAsync(Guid userId, bool isActive)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var (_, hotel) = await GetAdminWithHotelAsync(userId);
                if (isActive && hotel.IsBlockedBySuperAdmin)
                    throw new ValidationException("Hotel is blocked by SuperAdmin and cannot be activated.");
                hotel.IsActive = isActive;
                await _unitOfWork.CommitAsync();
                await _auditLogService.LogAsync(userId, isActive ? "HotelActivated" : "HotelDeactivated",
                    "Hotel", hotel.HotelId, $"IsActive set to {isActive}");
            }
            catch { await _unitOfWork.RollbackAsync(); throw; }
        }

        // ── ADMIN: UPDATE GST ─────────────────────────────────────────────────

        public async Task UpdateHotelGstAsync(Guid adminUserId, decimal gstPercent)
        {
            var (_, hotel) = await GetAdminWithHotelAsync(adminUserId);
            hotel.GstPercent = gstPercent;
            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync(adminUserId, "HotelGstUpdated", "Hotel",
                hotel.HotelId, $"GST set to {gstPercent}%");
        }

        // ── SUPERADMIN: BLOCK / UNBLOCK ───────────────────────────────────────

        public async Task BlockHotelAsync(Guid hotelId)
        {
            var hotel = await GetHotelOrThrowAsync(hotelId);
            hotel.IsBlockedBySuperAdmin = true;
            hotel.IsActive = false;
            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync(null, "HotelBlocked", "Hotel", hotelId, "Hotel blocked by SuperAdmin.");
        }

        public async Task UnblockHotelAsync(Guid hotelId)
        {
            var hotel = await GetHotelOrThrowAsync(hotelId);
            hotel.IsBlockedBySuperAdmin = false;
            await _unitOfWork.SaveChangesAsync();
            await _auditLogService.LogAsync(null, "HotelUnblocked", "Hotel", hotelId, "Hotel unblocked by SuperAdmin.");
        }

        // ── SUPERADMIN: LIST ALL HOTELS (non-paged) ───────────────────────────

        public async Task<IEnumerable<SuperAdminHotelListDto>> GetAllHotelsForSuperAdminAsync()
        {
            var hotels = await _hotelRepo.GetQueryable().AsNoTracking().OrderBy(h => h.Name).ToListAsync();
            var reservationCounts = await GetReservationCountsByHotelAsync(null);
            var revenueByHotel = await GetRevenueByHotelAsync(null);
            return hotels.Select(h => MapToSuperAdminDto(h, reservationCounts, revenueByHotel));
        }

        // ── SUPERADMIN: LIST ALL HOTELS (paged) ───────────────────────────────

        public async Task<PagedSuperAdminHotelResponseDto> GetAllHotelsForSuperAdminPagedAsync(
            int page, int pageSize, string? search = null, string? status = null)
        {
            var query = BuildSuperAdminHotelQuery(search, status);
            var totalCount = await query.CountAsync();
            var hotels = await query.OrderBy(h => h.Name)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var hotelIds = hotels.Select(h => h.HotelId).ToList();
            var reservationCounts = await GetReservationCountsByHotelAsync(hotelIds);
            var revenueByHotel = await GetRevenueByHotelAsync(hotelIds);

            return new PagedSuperAdminHotelResponseDto
            {
                TotalCount = totalCount,
                Hotels = hotels.Select(h => MapToSuperAdminDto(h, reservationCounts, revenueByHotel))
            };
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<(User user, Hotel hotel)> GetAdminWithHotelAsync(Guid userId)
        {
            var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (user.HotelId is null) throw new UnAuthorizedException("Unauthorized.");
            var hotel = await _hotelRepo.GetAsync(user.HotelId.Value)
                ?? throw new NotFoundException("Hotel not found.");
            return (user, hotel);
        }

        private async Task<Hotel> GetHotelOrThrowAsync(Guid hotelId)
            => await _hotelRepo.GetAsync(hotelId) ?? throw new NotFoundException("Hotel not found.");

        private IQueryable<Hotel> BuildPublicHotelQuery()
            => _hotelRepo.GetQueryable().AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin);

        private static IQueryable<Hotel> ApplySearchFilters(
            IQueryable<Hotel> query, SearchHotelRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.City))
                query = query.Where(h => h.City.ToLower() == request.City.ToLower());
            else if (!string.IsNullOrWhiteSpace(request.State))
                query = query.Where(h => h.State.ToLower() == request.State.ToLower());

            if (request.AmenityIds?.Count > 0)
                query = query.Where(h => h.RoomTypes!.Any(rt =>
                    rt.RoomTypeAmenities!.Any(rta => request.AmenityIds.Contains(rta.AmenityId))));

            if (!string.IsNullOrWhiteSpace(request.RoomType))
                query = query.Where(h => h.RoomTypes!.Any(rt =>
                    rt.Name.ToLower().Contains(request.RoomType.ToLower())));

            if (request.MinPrice.HasValue)
                query = query.Where(h => h.RoomTypes!.SelectMany(rt => rt.Rates!)
                    .Any(r => r.Rate >= request.MinPrice.Value));

            if (request.MaxPrice.HasValue)
                query = query.Where(h => h.RoomTypes!.SelectMany(rt => rt.Rates!)
                    .Any(r => r.Rate <= request.MaxPrice.Value));

            return query;
        }

        private static IQueryable<Hotel> ApplySorting(IQueryable<Hotel> query, string? sortBy)
            => sortBy switch
            {
                "price_asc"  => query.OrderBy(h => h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate) ?? 0),
                "price_desc" => query.OrderByDescending(h => h.RoomTypes!.SelectMany(rt => rt.Rates!).Min(r => (decimal?)r.Rate) ?? 0),
                _            => query.OrderBy(h => h.Name)
            };

        private static SearchHotelResponseDto EmptySearchResponse(int pageNumber) => new()
        {
            Hotels = new List<HotelListItemDto>(),
            PageNumber = pageNumber,
            RecordsCount = 0,
            TotalCount = 0
        };

        private async Task<IEnumerable<HotelListItemDto>> FetchHotelListAsync(
            System.Linq.Expressions.Expression<Func<Hotel, bool>> filter, int? take = null)
        {
            var query = _hotelRepo.GetQueryable().AsNoTracking()
                .Where(h => h.IsActive && !h.IsBlockedBySuperAdmin)
                .Where(filter)
                .Select(h => new
                {
                    h.HotelId, h.Name, h.City, h.ImageUrl,
                    AvgRating = h.Reviews!.Select(r => (decimal?)r.Rating).Average(),
                    ReviewCount = h.Reviews!.Count(),
                    StartingPrice = h.RoomTypes!.SelectMany(rt => rt.Rates!).Select(r => (decimal?)r.Rate).Min()
                })
                .OrderByDescending(h => h.AvgRating ?? 0);

            var raw = take.HasValue
                ? await query.Take(take.Value).ToListAsync()
                : await query.ToListAsync();

            return raw.Select(h => MapToListItemDto(
                h.HotelId, h.Name, h.City, h.ImageUrl,
                h.AvgRating, h.ReviewCount, h.StartingPrice));
        }

        private IQueryable<Hotel> BuildSuperAdminHotelQuery(string? search, string? status)
        {
            var query = _hotelRepo.GetQueryable().AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(h => h.Name.Contains(search) || h.City.Contains(search));
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                query = status switch
                {
                    "Active"   => query.Where(h => h.IsActive && !h.IsBlockedBySuperAdmin),
                    "Inactive" => query.Where(h => !h.IsActive && !h.IsBlockedBySuperAdmin),
                    "Blocked"  => query.Where(h => h.IsBlockedBySuperAdmin),
                    _          => query
                };
            return query;
        }

        private async Task<Dictionary<Guid, int>> GetReservationCountsByHotelAsync(List<Guid>? hotelIds)
        {
            var query = _reservationRepo.GetQueryable().AsNoTracking();
            if (hotelIds is not null) query = query.Where(r => hotelIds.Contains(r.HotelId));
            return await query.GroupBy(r => r.HotelId)
                .Select(g => new { HotelId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.HotelId, x => x.Count);
        }

        private async Task<Dictionary<Guid, decimal>> GetRevenueByHotelAsync(List<Guid>? hotelIds)
        {
            var query = _transactionRepo.GetQueryable().AsNoTracking()
                .Where(t => t.Status == PaymentStatus.Success);
            if (hotelIds is not null) query = query.Where(t => hotelIds.Contains(t.Reservation!.HotelId));
            return await query.GroupBy(t => t.Reservation!.HotelId)
                .Select(g => new { HotelId = g.Key, Revenue = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(x => x.HotelId, x => x.Revenue);
        }

        private static void ApplyHotelUpdates(Hotel hotel, UpdateHotelDto dto)
        {
            hotel.Name = dto.Name;
            hotel.Address = dto.Address;
            hotel.City = dto.City;
            if (!string.IsNullOrWhiteSpace(dto.State)) hotel.State = dto.State;
            hotel.Description = dto.Description;
            hotel.ContactNumber = dto.ContactNumber;
            hotel.ImageUrl = dto.ImageUrl;
            if (dto.UpiId is not null) hotel.UpiId = dto.UpiId;
        }

        private static HotelDetailsDto BuildHotelDetailsDto(Hotel hotel)
        {
            var reviews = hotel.Reviews ?? new List<Review>();
            var allAmenities = hotel.RoomTypes?
                .SelectMany(rt => rt.RoomTypeAmenities ?? Enumerable.Empty<RoomTypeAmenity>())
                .Select(rta => rta.Amenity?.Name ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList()
                ?? new List<string>();

            return new HotelDetailsDto
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Address = hotel.Address,
                City = hotel.City,
                State = hotel.State,
                Description = hotel.Description,
                ImageUrl = hotel.ImageUrl,
                ContactNumber = hotel.ContactNumber,
                UpiId = hotel.UpiId,
                GstPercent = hotel.GstPercent,
                AverageRating = reviews.Any()
                    ? Math.Round(reviews.Average(r => (decimal)r.Rating), 2) : 0m,
                ReviewCount = reviews.Count,
                Amenities = allAmenities,
                RoomTypes = hotel.RoomTypes?.Select(t => MapToRoomTypePublicDto(t))
                    ?? Enumerable.Empty<RoomTypePublicDto>(),
                Reviews = reviews.OrderByDescending(r => r.CreatedDate).Take(10).Select(r => new ReviewDto
                {
                    UserName = r.User?.Name ?? "Anonymous",
                    UserProfileImageUrl = r.User?.UserDetails?.ProfileImageUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    ImageUrl = r.ImageUrl,
                    AdminReply = r.AdminReply,
                    CreatedDate = r.CreatedDate
                })
            };
        }

        private static RoomTypePublicDto MapToRoomTypePublicDto(RoomType roomType) => new()
        {
            RoomTypeId = roomType.RoomTypeId,
            Name = roomType.Name,
            Description = roomType.Description,
            MaxOccupancy = roomType.MaxOccupancy,
            Amenities = roomType.RoomTypeAmenities?
                .Select(rta => rta.Amenity?.Name ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                ?? Enumerable.Empty<string>(),
            AmenityList = roomType.RoomTypeAmenities?.Select(rta => new AmenityPublicDto
            {
                AmenityId = rta.AmenityId,
                Name = rta.Amenity?.Name ?? string.Empty,
                Category = rta.Amenity?.Category ?? string.Empty,
                IconName = rta.Amenity?.IconName
            }) ?? Enumerable.Empty<AmenityPublicDto>(),
            ImageUrl = roomType.ImageUrl
        };

        private static SuperAdminHotelListDto MapToSuperAdminDto(
            Hotel hotel,
            Dictionary<Guid, int> reservationCounts,
            Dictionary<Guid, decimal> revenueByHotel) => new()
        {
            HotelId = hotel.HotelId,
            Name = hotel.Name,
            City = hotel.City,
            ContactNumber = hotel.ContactNumber,
            IsActive = hotel.IsActive,
            IsBlockedBySuperAdmin = hotel.IsBlockedBySuperAdmin,
            CreatedAt = hotel.CreatedAt,
            TotalReservations = reservationCounts.TryGetValue(hotel.HotelId, out var rc) ? rc : 0,
            TotalRevenue = revenueByHotel.TryGetValue(hotel.HotelId, out var rv) ? rv : 0m
        };

        private static HotelListItemDto MapToListItemDto(
            Guid hotelId, string name, string city, string? imageUrl,
            decimal? avgRating, int reviewCount, decimal? startingPrice) => new()
        {
            HotelId = hotelId,
            Name = name,
            City = city,
            ImageUrl = imageUrl ?? string.Empty,
            AverageRating = Math.Round(avgRating ?? 0m, 2),
            ReviewCount = reviewCount,
            StartingPrice = startingPrice ?? 0
        };
    }
}
