using HotelBookingAppWebApi.Models.DTOs.Room;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Physical room management for hotel admins.</summary>
    public interface IRoomService
    {
        /// <summary>Adds a new room to the admin's hotel. Enforces room-type inventory cap.</summary>
        Task AddRoomAsync(Guid userId, CreateRoomDto dto);

        /// <summary>Updates room number, floor, or room type.</summary>
        Task UpdateRoomAsync(Guid userId, UpdateRoomDto dto);

        /// <summary>Activates or deactivates a room.</summary>
        Task ToggleRoomStatusAsync(Guid userId, Guid roomId, bool isActive);

        /// <summary>Returns paged list of all rooms in the admin's hotel.</summary>
        Task<IEnumerable<RoomListResponseDto>> GetRoomsByHotelAsync(
            Guid userId, int pageNumber, int pageSize);

        /// <summary>Returns the total room count for the admin's hotel (used for pagination).</summary>
        Task<int> GetRoomCountByHotelAsync(Guid userId);
    }
}
