using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.QueryModels;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class HotelService : IHotelService
    {
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly HotelBookingContext _context; // for Stored Procedures
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        public HotelService(
                IRepository<Guid, Hotel> hotelRepo,
                IRepository<Guid, User> userRepo,
                IRepository<Guid, RoomType> roomTypeRepo,
                HotelBookingContext context)
        {
            _hotelRepo = hotelRepo;
            _userRepo = userRepo;
            _roomTypeRepo = roomTypeRepo;
            _context = context;
        }


        // ✅ TOP HOTELS (Stored Procedure)
        public async Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync()
        {
            var result = await _context.Set<TopHotelView>()
                .FromSqlRaw("EXEC proc_GetTopHotels")
                .ToListAsync();

            return result.Select(h => new HotelListItemDto
            {
                HotelId = h.HotelId,
                Name = h.Name,
                City = h.City,
                ImageUrl = h.ImageUrl,
                AverageRating = h.AverageRating,
                ReviewCount = h.ReviewCount,
                StartingPrice = h.StartingPrice
            });
        }

        // ✅ SEARCH HOTELS (Stored Procedure)
        public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
        {
            var offset = (request.PageNumber - 1) * request.PageSize;

            var hotels = await _context.Hotels
                .FromSqlRaw("EXEC proc_SearchHotels {0},{1},{2},{3},{4}",
                    request.City,
                    offset,
                    request.PageSize,
                    request.CheckIn,
                    request.CheckOut)
                .ToListAsync();

            if (!hotels.Any())
                throw new NotFoundException("No hotels found");

            return new SearchHotelResponseDto
            {
                Hotels = hotels.Select(h => new HotelListItemDto
                {
                    HotelId = h.HotelId,
                    Name = h.Name,
                    City = h.City,
                    ImageUrl = h.ImageUrl
                }),
                PageNumber = request.PageNumber,
                RecordsCount = hotels.Count
            };
        }

        // ✅ HOTEL DETAILS (IQueryable + Include)
        public async Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId)
        {
            var hotel = await _hotelRepo.GetQueryable()
                .Include(h => h.RoomTypes)
                .Include(h => h.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(h => h.HotelId == hotelId);

            if (hotel == null)
                throw new NotFoundException("Hotel not found");

            var reviews = hotel.Reviews ?? new List<Review>();

            var amenities = hotel.RoomTypes?
                .Where(rt => !string.IsNullOrEmpty(rt.Amenities))
                .SelectMany(rt => rt.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(a => a.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            return new HotelDetailsDto
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Address = hotel.Address,
                City = hotel.City,
                Description = hotel.Description,
                AverageRating = reviews.Any()
                    ? Math.Round(reviews.Average(r => r.Rating), 2)
                    : 0,
                Amenities = amenities,
                Reviews = reviews.Select(r => new ReviewDto
                {
                    UserName = r.User?.Name ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedDate = r.CreatedDate
                })
            };
        }

        //  ROOM TYPES
        public async Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId)
        {
            var types = await _roomTypeRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId && r.IsActive)
                .ToListAsync();

            return types.Select(t => new RoomTypePublicDto
            {
                RoomTypeId = t.RoomTypeId,
                Name = t.Name,
                Description = t.Description,
                MaxOccupancy = t.MaxOccupancy,
                Amenities = t.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries)
            });
        }


        // ✅ AVAILABILITY
        public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(
            Guid hotelId,
            DateOnly checkIn,
            DateOnly checkOut)
        {
            var inventories = await _context.RoomTypeInventories
                .Include(i => i.RoomType)
                    .ThenInclude(rt => rt!.Rates)
                .Where(i =>
                    i.RoomType!.HotelId == hotelId &&
                    i.Date >= checkIn &&
                    i.Date <= checkOut)
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

        // ✅ UPDATE HOTEL
        public async Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto)
        {
            var user = await _userRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized");

            var hotel = await _hotelRepo.GetAsync(user.HotelId.Value);

            if (hotel == null)
                throw new NotFoundException("Hotel not found");

            hotel.Name = dto.Name;
            hotel.Address = dto.Address;
            hotel.City = dto.City;
            hotel.Description = dto.Description;
            hotel.ContactNumber = dto.ContactNumber;
            hotel.ImageUrl = dto.ImageUrl;

            await _hotelRepo.UpdateAsync(hotel.HotelId, hotel);
        }

        // ✅ TOGGLE STATUS
        public async Task ToggleHotelStatusAsync(Guid userId, bool isActive)
        {
            var user = await _userRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized");

            var hotel = await _hotelRepo.GetAsync(user.HotelId.Value);

            if (hotel == null)
                throw new NotFoundException("Hotel not found");

            hotel.IsActive = isActive;

            await _hotelRepo.UpdateAsync(hotel.HotelId, hotel);
        }
    }
}
