using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Log
{
    public class CreateLogDto
    {
        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string ErrorNumber { get; set; } = string.Empty;
    }
}
