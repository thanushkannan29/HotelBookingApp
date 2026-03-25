using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class AmenityRequest
    {
        [Key]
        public Guid AmenityRequestId { get; set; }

        [Required]
        public Guid RequestedByAdminId { get; set; }

        [Required]
        public Guid AdminHotelId { get; set; }

        [Required, MaxLength(200)]
        public string AmenityName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? IconName { get; set; }

        public AmenityRequestStatus Status { get; set; } = AmenityRequestStatus.Pending;

        [MaxLength(500)]
        public string? SuperAdminNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public User? RequestedByAdmin { get; set; }
    }

    public enum AmenityRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}
