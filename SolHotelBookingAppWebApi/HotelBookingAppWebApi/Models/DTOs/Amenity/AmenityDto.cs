using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Amenity
{
    public class AmenityResponseDto
    {
        public Guid AmenityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? IconName { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateAmenityDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? IconName { get; set; }
    }

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