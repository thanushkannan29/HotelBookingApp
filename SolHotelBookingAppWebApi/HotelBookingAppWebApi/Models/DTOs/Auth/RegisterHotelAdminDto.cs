using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Auth
{
    /// <summary>Registration payload for a new hotel admin — creates both the user and the hotel.</summary>
    public class RegisterHotelAdminDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string HotelName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string ContactNumber { get; set; } = string.Empty;
    }
}
