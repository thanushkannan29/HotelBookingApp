using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.AmenityRequest
{
    public class CreateAmenityRequestDto
    {
        [Required, MaxLength(200)]
        public string AmenityName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? IconName { get; set; }
    }

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

    public class PagedAmenityRequestResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<AmenityRequestResponseDto> Requests { get; set; } = new List<AmenityRequestResponseDto>();
    }

    public class RejectAmenityRequestDto
    {
        [Required]
        public string Note { get; set; } = string.Empty;
    }
}
