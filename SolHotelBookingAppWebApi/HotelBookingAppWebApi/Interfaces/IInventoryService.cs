using HotelBookingAppWebApi.Models.DTOs.Inventory;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IInventoryService
    {
        Task AddInventoryAsync(Guid userId, CreateInventoryDto dto);

        Task UpdateInventoryAsync(Guid userId, UpdateInventoryDto dto);

        Task AdjustReservedInventoryAsync(Guid userId, AdjustReservedInventoryDto dto);

        Task<IEnumerable<InventoryResponseDto>> GetInventoryAsync(
            Guid userId, Guid roomTypeId, DateOnly start, DateOnly end);
    }
}
