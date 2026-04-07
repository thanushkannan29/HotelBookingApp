using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Amenity
{
    /// <summary>Payload for updating an existing amenity (SuperAdmin only).</summary>
    public class UpdateAmenityDto
    {
        [Required]
        public Guid AmenityId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? IconName { get; set; }
        public bool IsActive { get; set; }
    }
}
