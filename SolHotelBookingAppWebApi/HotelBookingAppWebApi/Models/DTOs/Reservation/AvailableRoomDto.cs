namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>A room available for selection during booking.</summary>
    public class AvailableRoomDto
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
    }
}
