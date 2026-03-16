using HotelBookingAppWebApi.Models;

namespace HotelBookingAppWebApi.Interfaces.Repository
{
    public interface IReservationRepository
    {
        Task<RoomType?> GetRoomTypeAsync(Guid roomTypeId, Guid hotelId);

        Task<int> GetPhysicalRoomsAsync(Guid roomTypeId, Guid hotelId);

        Task<List<RoomTypeInventory>> GetInventoriesAsync(Guid roomTypeId, List<DateOnly> dates);

        Task<List<RoomTypeRate>> GetRatesAsync(Guid roomTypeId, DateOnly checkIn, DateOnly checkOut);

        Task<List<Room>> GetAvailableRoomsAsync(Guid roomTypeId, Guid hotelId);

        Task AddReservationAsync(Reservation reservation);

        Task AddReservationRoomAsync(ReservationRoom room);

        Task<Reservation?> GetReservationByCodeAsync(string code, Guid userId);

        Task<List<Reservation>> GetUserReservationsAsync(Guid userId);

        Task<Reservation?> GetReservationForCancelAsync(string code, Guid userId);

        Task<Reservation?> GetReservationForAdminAsync(string code);

        Task SaveAsync();
    }
}
