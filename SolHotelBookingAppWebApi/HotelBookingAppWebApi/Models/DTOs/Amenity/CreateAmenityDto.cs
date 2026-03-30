using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Amenity
{
    /// <summary>Payload for creating a new amenity (SuperAdmin only).</summary>
    public class CreateAmenityDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? IconName { get; set; }
    }
}
