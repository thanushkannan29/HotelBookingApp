using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.SupportRequest
{
    public class RespondSupportRequestDto
    {
        [MaxLength(2000)] public string Response { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Status { get; set; } = "Resolved";
    }
}
