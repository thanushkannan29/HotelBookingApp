namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    public class ReservationResponseDto
    {
        public string ReservationCode { get; set; } = string.Empty;
        public Guid ReservationId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        
    }
}
