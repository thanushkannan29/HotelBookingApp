namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class RoomAvailabilityDto
    {
        public Guid RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int AvailableRooms { get; set; }
        public string? ImageUrl { get; set; }
    }
}
