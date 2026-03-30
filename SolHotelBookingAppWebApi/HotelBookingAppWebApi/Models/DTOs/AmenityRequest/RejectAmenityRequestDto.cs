using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.AmenityRequest
{
    /// <summary>Payload for SuperAdmin rejecting an amenity request with a note.</summary>
    public class RejectAmenityRequestDto
    {
        [Required]
        public string Note { get; set; } = string.Empty;
    }
}
