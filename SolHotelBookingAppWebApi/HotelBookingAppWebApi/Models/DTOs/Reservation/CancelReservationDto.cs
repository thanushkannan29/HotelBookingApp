using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    public class CancelReservationDto
    {
        
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
