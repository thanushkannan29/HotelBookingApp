using HotelBookingAppWebApi.Models.DTOs.Inventory;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Room type inventory management — date-range availability slots.</summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Adds inventory records for each date in the range.
        /// Skips dates that already have inventory (idempotent bulk insert).
        /// </summary>
        Task AddInventoryAsync(Guid userId, CreateInventoryDto dto);

        /// <summary>Updates the total inventory for a single date. Throws if new total is below reserved count.</summary>
        Task UpdateInventoryAsync(Guid userId, UpdateInventoryDto dto);

        /// <summary>Returns inventory records for a room type between two dates, ordered by date.</summary>
        Task<IEnumerable<InventoryResponseDto>> GetInventoryAsync(
            Guid userId, Guid roomTypeId, DateOnly start, DateOnly end);
    }
}
