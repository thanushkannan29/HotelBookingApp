namespace HotelBookingAppWebApi.Models.DTOs.Amenity
{
    /// <summary>Amenity details returned to the client.</summary>
    public class AmenityResponseDto
    {
        public Guid AmenityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? IconName { get; set; }
        public bool IsActive { get; set; }
    }
}
