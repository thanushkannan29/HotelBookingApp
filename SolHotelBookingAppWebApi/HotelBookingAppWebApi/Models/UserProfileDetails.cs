using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class UserProfileDetails
    {
        [Key]
        public Guid UserDetailsId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Pincode { get; set; } = string.Empty;

        /// <summary>Optional profile image URL</summary>
        public string? ProfileImageUrl { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
