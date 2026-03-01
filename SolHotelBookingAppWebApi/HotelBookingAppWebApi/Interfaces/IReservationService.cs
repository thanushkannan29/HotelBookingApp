using HotelBookingAppWebApi.Models.DTOs.Reservation;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IReservationService
    {
        Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto);

        Task<ReservationDetailsDto> GetReservationByCodeAsync(Guid userId, string reservationCode);

        Task<IEnumerable<ReservationDetailsDto>> GetMyReservationsAsync(Guid userId);

        Task<bool> CancelReservationAsync(Guid userId, string reservationCode, string reason);

        Task<bool> CompleteReservationAsync(string reservationCode); // Admin use
    }
}
