using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Auth
{
    public class RegisterHotelAdminDto
    {
        // Admin info
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        // Hotel info
        [Required]
        public string HotelName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

}
