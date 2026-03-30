namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class AmenityItemDto
    {
        public Guid AmenityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? IconName { get; set; }
    }
}
