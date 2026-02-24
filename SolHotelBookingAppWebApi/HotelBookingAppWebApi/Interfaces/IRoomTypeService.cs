using HotelBookingAppWebApi.Models.DTOs.RoomType;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IRoomTypeService
    {
        Task AddRoomTypeAsync(Guid userId, CreateRoomTypeDto dto);

        Task UpdateRoomTypeAsync(Guid userId, UpdateRoomTypeDto dto);

        

        Task ToggleRoomTypeStatusAsync(Guid userId, Guid roomTypeId, bool isActive);


        Task AddRateAsync(Guid userId, CreateRoomTypeRateDto dto);

        Task UpdateRateAsync(Guid userId, UpdateRoomTypeRateDto dto);

        Task<decimal> GetRateByDateAsync(Guid userId, GetRateByDateRequestDto dto);
    }
}
