namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    public class ReservationDetailsDto
    {
        public string ReservationCode { get; set; } = string.Empty;
        public Guid HotelId { get; set; }
        public Guid RoomTypeId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumberOfRooms { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

}
