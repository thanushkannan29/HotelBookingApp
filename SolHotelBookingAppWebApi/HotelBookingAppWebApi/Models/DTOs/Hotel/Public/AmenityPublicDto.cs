namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class AmenityPublicDto
    {
        public Guid AmenityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? IconName { get; set; }
    }
}
