using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>Payload for cancelling a reservation.</summary>
    public class CancelReservationDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
