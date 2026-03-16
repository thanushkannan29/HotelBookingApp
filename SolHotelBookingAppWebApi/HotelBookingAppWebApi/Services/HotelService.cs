using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.Repository;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using Microsoft.EntityFrameworkCore;
namespace HotelBookingAppWebApi.Services
{
    public class HotelService : IHotelService
    {
        private readonly IHotelRepository _hotelRepository;
        private readonly IRepository<Guid, User> _userRepository;

        public HotelService(
            IHotelRepository hotelRepository,
            IRepository<Guid, User> userRepository)
        {
            _hotelRepository = hotelRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync()
        {
            var result = await _hotelRepository.GetTopHotelsAsync();

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

        public async Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request)
        {
            var offset = (request.PageNumber - 1) * request.PageSize;

            var hotels = await _hotelRepository.SearchHotelsAsync(
                request.City,
                offset,
                request.PageSize,
                request.CheckIn,
                request.CheckOut);

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
                RecordsCount = hotels.Count()
            };
        }

        public async Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId)
        {
            var hotel = await _hotelRepository.GetHotelDetailsAsync(hotelId);

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

        public async Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId)
        {
            var types = await _hotelRepository.GetRoomTypesAsync(hotelId);

            return types.Select(t => new RoomTypePublicDto
            {
                RoomTypeId = t.RoomTypeId,
                Name = t.Name,
                Description = t.Description,
                MaxOccupancy = t.MaxOccupancy,
                Amenities = t.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries)
            });
        }

        public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(
            Guid hotelId,
            DateOnly checkIn,
            DateOnly checkOut)
        {
            var inventories = await _hotelRepository.GetAvailabilityAsync(hotelId, checkIn, checkOut);

            return inventories
                .GroupBy(i => i.RoomType!)
                .Select(g => new RoomAvailabilityDto
                {
                    RoomTypeId = g.Key.RoomTypeId,
                    RoomTypeName = g.Key.Name,
                    PricePerNight = g.Key.Rates?.FirstOrDefault()?.Rate ?? 0,
                    AvailableRooms = g.Min(x => x.AvailableInventory)
                });
        }

        public async Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto)
        {
            var user = (await _userRepository.FindAsync(u => u.UserId == userId)).FirstOrDefault();

            if (user == null || user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized");

            var hotel = await _hotelRepository.GetByIdAsync(user.HotelId.Value);

            if (hotel == null)
                throw new NotFoundException("Hotel not found");

            hotel.Name = dto.Name;
            hotel.Address = dto.Address;
            hotel.City = dto.City;
            hotel.Description = dto.Description;
            hotel.ContactNumber = dto.ContactNumber;
            hotel.ImageUrl = dto.ImageUrl;

            await _hotelRepository.UpdateAsync(hotel.HotelId, hotel);
        }

        public async Task ToggleHotelStatusAsync(Guid userId, bool isActive)
        {
            var user = (await _userRepository.FindAsync(u => u.UserId == userId)).FirstOrDefault();

            if (user == null || user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized");

            var hotel = await _hotelRepository.GetByIdAsync(user.HotelId.Value);

            if (hotel == null)
                throw new NotFoundException("Hotel not found");

            hotel.IsActive = isActive;

            await _hotelRepository.UpdateAsync(hotel.HotelId, hotel);
        }
    }
}
