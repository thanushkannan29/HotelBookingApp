using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Room;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Full reservation lifecycle — creation, cancellation, confirmation, and queries.</summary>
    public interface IReservationService
    {
        /// <summary>Creates a new reservation, deducts wallet if applicable, and locks inventory.</summary>
        Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto);

        /// <summary>Returns full details of a single reservation by its code. Guest-scoped.</summary>
        Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string reservationCode);

        /// <summary>Returns all reservations for the authenticated guest (no pagination).</summary>
        Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId);

        /// <summary>Returns paged reservation history for the authenticated guest with optional status/search filters.</summary>
        Task<PagedReservationResponseDto> GetMyReservationsPagedAsync(
            Guid userId, int page, int pageSize, string? status = null, string? search = null);

        /// <summary>Cancels a reservation and issues a refund to wallet based on the cancellation policy.</summary>
        Task<bool> CancelReservationAsync(Guid userId, string reservationCode, string reason);

        /// <summary>Admin marks a confirmed reservation as completed and triggers commission recording.</summary>
        Task<bool> CompleteReservationAsync(string reservationCode);

        /// <summary>Admin confirms a pending reservation.</summary>
        Task<bool> ConfirmReservationAsync(string reservationCode);

        /// <summary>Returns paged reservations for the admin's hotel with optional status/search/sort.</summary>
        Task<PagedReservationResponseDto> GetAdminReservationsAsync(
            Guid adminUserId, string? status, string? search,
            int page, int pageSize, string? sortField = null, string? sortDir = null);

        /// <summary>Returns rooms available for booking for a given hotel, room type, and date range.</summary>
        Task<IEnumerable<AvailableRoomDto>> GetAvailableRoomsAsync(
            Guid hotelId, Guid roomTypeId, DateOnly checkIn, DateOnly checkOut);

        /// <summary>Returns all rooms in the admin's hotel with their occupancy status for a given date.</summary>
        Task<IEnumerable<RoomOccupancyDto>> GetRoomOccupancyAsync(Guid adminUserId, DateOnly date);

        /// <summary>Returns the UPI QR code payload for a pending reservation payment.</summary>
        Task<QrPaymentResponseDto> GetPaymentQrAsync(Guid userId, Guid reservationId);
    }
}
