using HotelBookingAppWebApi.Models.DTOs.Room;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IRoomService
    {
        Task AddRoomAsync(Guid userId, CreateRoomDto dto);

        Task UpdateRoomAsync(Guid userId, UpdateRoomDto dto);

        Task ToggleRoomStatusAsync(Guid userId, Guid roomId, bool isActive);

        Task<IEnumerable<RoomListResponseDto>> GetRoomsByHotelAsync(Guid userId, int pageNumber, int pageSize);
    }
}
