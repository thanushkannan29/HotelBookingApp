using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.SupportRequest
{
    public class AdminSupportRequestDto
    {
        [Required, MaxLength(100)] public string Subject { get; set; } = string.Empty;
        [Required, MaxLength(2000)] public string Message { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Category { get; set; } = string.Empty;
    }
}
