namespace HotelBookingAppWebApi.Models.DTOs.Room
{
    public class RoomOccupancyDto
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public string? ReservationCode { get; set; }
    }
}
