using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.SupportRequest
{
    public class GuestSupportRequestDto
    {
        [Required, MaxLength(100)] public string Subject { get; set; } = string.Empty;
        [Required, MaxLength(2000)] public string Message { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Category { get; set; } = string.Empty;
        [MaxLength(50)] public string? ReservationCode { get; set; }
        public Guid? HotelId { get; set; }
    }
}
