using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Admin
{
    public class UpdateHotelGstDto
    {
        [Required]
        [Range(0, 28, ErrorMessage = "GST must be between 0 and 28")]
        public decimal GstPercent { get; set; }
    }
}
