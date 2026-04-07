using HotelBookingAppWebApi.Models.DTOs.RoomType;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Room type management including amenity associations and date-based pricing rates.</summary>
    public interface IRoomTypeService
    {
        /// <summary>Creates a new room type with optional amenity associations.</summary>
        Task AddRoomTypeAsync(Guid userId, CreateRoomTypeDto dto);

        /// <summary>Updates room type details and replaces its amenity associations.</summary>
        Task UpdateRoomTypeAsync(Guid userId, UpdateRoomTypeDto dto);

        /// <summary>Activates or deactivates a room type.</summary>
        Task ToggleRoomTypeStatusAsync(Guid userId, Guid roomTypeId, bool isActive);

        /// <summary>Adds a date-range pricing rate. Throws on overlapping ranges.</summary>
        Task AddRateAsync(Guid userId, CreateRoomTypeRateDto dto);

        /// <summary>Updates an existing pricing rate's dates and amount.</summary>
        Task UpdateRateAsync(Guid userId, UpdateRoomTypeRateDto dto);

        /// <summary>Returns the applicable rate for a specific date.</summary>
        Task<decimal> GetRateByDateAsync(Guid userId, GetRateByDateRequestDto dto);

        /// <summary>Returns all pricing rates for a room type ordered by start date.</summary>
        Task<IEnumerable<RoomTypeRateDto>> GetRatesAsync(Guid userId, Guid roomTypeId);

        /// <summary>Returns all room types for the admin's hotel (no pagination).</summary>
        Task<IEnumerable<RoomTypeListDto>> GetRoomTypesByHotelAsync(Guid userId);

        /// <summary>Returns paged room types for the admin's hotel.</summary>
        Task<PagedRoomTypeResponseDto> GetRoomTypesByHotelPagedAsync(Guid userId, int page, int pageSize);
    }
}
