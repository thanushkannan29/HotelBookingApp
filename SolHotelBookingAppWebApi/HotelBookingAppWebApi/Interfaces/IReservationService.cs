using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Room;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IReservationService
    {
        Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto);
        Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string reservationCode);
        Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId);
        Task<PagedReservationResponseDto> GetMyReservationsPagedAsync(Guid userId, int page, int pageSize);
        Task<bool> CancelReservationAsync(Guid userId, string reservationCode, string reason);
        Task<bool> CompleteReservationAsync(string reservationCode);
        Task<PagedReservationResponseDto> GetHotelReservationsAsync(Guid userId, int page, int pageSize);
        Task<IEnumerable<AvailableRoomDto>> GetAvailableRoomsAsync(Guid hotelId, Guid roomTypeId, DateOnly checkIn, DateOnly checkOut);

        /// <summary>For a given hotel and date, return occupancy status for every physical room</summary>
        Task<IEnumerable<RoomOccupancyDto>> GetRoomOccupancyAsync(Guid adminUserId, DateOnly date);
    }
}