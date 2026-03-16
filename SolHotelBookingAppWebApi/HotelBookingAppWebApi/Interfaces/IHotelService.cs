using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IHotelService
    {
        Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync();

        Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request);

        Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId);

        Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId);

        Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(
            Guid hotelId,
            DateOnly checkIn,
            DateOnly checkOut);

        Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto);

        Task ToggleHotelStatusAsync(Guid userId, bool isActive);
    }
}
