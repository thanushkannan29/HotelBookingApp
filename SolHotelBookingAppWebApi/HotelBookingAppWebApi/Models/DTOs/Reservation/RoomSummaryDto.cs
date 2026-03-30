namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>Compact room info included in reservation responses.</summary>
    public class RoomSummaryDto
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
    }
}
