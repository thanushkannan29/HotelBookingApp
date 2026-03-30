using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.UserDetails
{
    public class UpdateUserProfileDto
    {
        [MaxLength(150)] public string? Name { get; set; }
        [MaxLength(15)] public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Pincode { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
