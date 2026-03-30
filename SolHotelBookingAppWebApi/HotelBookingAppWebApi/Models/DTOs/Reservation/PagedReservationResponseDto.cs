namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>Paged wrapper for reservation list responses.</summary>
    public class PagedReservationResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<ReservationDetailsDto> Reservations { get; set; } = new List<ReservationDetailsDto>();
    }
}
