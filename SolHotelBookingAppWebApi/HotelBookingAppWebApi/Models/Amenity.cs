using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Amenity
    {
        [Key]
        public Guid AmenityId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Category e.g. 'Room', 'Bathroom', 'Tech', 'Services', 'Food'</summary>
        [Required]
        public string Category { get; set; } = string.Empty;

        /// <summary>Material icon name for the frontend</summary>
        public string? IconName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}