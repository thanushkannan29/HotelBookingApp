namespace HotelBookingAppWebApi.Models.DTOs.AmenityRequest
{
    /// <summary>Amenity request details returned to admin and SuperAdmin.</summary>
    public class AmenityRequestResponseDto
    {
        public Guid AmenityRequestId { get; set; }
        public string AmenityName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? IconName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SuperAdminNote { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
