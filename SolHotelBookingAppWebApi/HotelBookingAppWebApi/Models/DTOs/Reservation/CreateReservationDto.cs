using System.ComponentModel.DataAnnotations;
using HotelBookingAppWebApi.Models;

namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    public class CreateReservationDto
    {
        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateOnly CheckInDate { get; set; }

        [Required]
        public DateOnly CheckOutDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of rooms must be at least 1")]
        public int NumberOfRooms { get; set; }

        
    }
}
