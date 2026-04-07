using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.AmenityRequest
{
    /// <summary>Payload for an admin submitting a new amenity request to SuperAdmin.</summary>
    public class CreateAmenityRequestDto
    {
        [Required, MaxLength(200)]
        public string AmenityName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? IconName { get; set; }
    }
}
