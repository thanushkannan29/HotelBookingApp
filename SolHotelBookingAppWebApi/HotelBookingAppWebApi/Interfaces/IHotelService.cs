using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IHotelService
    {
        Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request);

        Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId);

        Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto);
        Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync();

        Task ToggleHotelStatusAsync(Guid userId, bool isActive);
    }
}
