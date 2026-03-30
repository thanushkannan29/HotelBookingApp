namespace HotelBookingAppWebApi.Models.DTOs.UserDetails
{
    public class BookingHistoryDto
    {
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
