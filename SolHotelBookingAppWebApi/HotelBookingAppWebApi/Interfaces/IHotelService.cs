using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.DTOs.Hotel.SuperAdmin;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IHotelService
    {
        // ── Public ────────────────────────────────────────────────────────────
        Task<IEnumerable<HotelListItemDto>> GetTopHotelsAsync();
        Task<SearchHotelResponseDto> SearchHotelsAsync(SearchHotelRequestDto request);
        Task<HotelDetailsDto> GetHotelDetailsAsync(Guid hotelId);
        Task<IEnumerable<RoomTypePublicDto>> GetRoomTypesAsync(Guid hotelId);
        Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilityAsync(Guid hotelId, DateOnly checkIn, DateOnly checkOut);
        Task<IEnumerable<string>> GetCitiesAsync();
        Task<IEnumerable<HotelListItemDto>> GetHotelsByCityAsync(string city);
        Task<IEnumerable<string>> GetActiveStatesAsync();
        Task<IEnumerable<HotelListItemDto>> GetHotelsByStateAsync(string stateName);

        // ── Admin ─────────────────────────────────────────────────────────────
        Task UpdateHotelAsync(Guid userId, UpdateHotelDto dto);
        Task ToggleHotelStatusAsync(Guid userId, bool isActive);
        Task UpdateHotelGstAsync(Guid adminUserId, decimal gstPercent);

        // ── SuperAdmin ────────────────────────────────────────────────────────
        Task<IEnumerable<SuperAdminHotelListDto>> GetAllHotelsForSuperAdminAsync();
        Task<PagedSuperAdminHotelResponseDto> GetAllHotelsForSuperAdminPagedAsync(int page, int pageSize, string? search = null, string? status = null);
        Task BlockHotelAsync(Guid hotelId);
        Task UnblockHotelAsync(Guid hotelId);
    }
}