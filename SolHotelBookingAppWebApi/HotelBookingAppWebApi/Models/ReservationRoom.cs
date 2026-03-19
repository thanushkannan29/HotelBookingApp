using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class ReservationRoom
    {
        [Key]
        public Guid ReservationRoomId { get; set; }

        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public decimal PricePerNight { get; set; }

        public Reservation? Reservation { get; set; }
        public RoomType? RoomType { get; set; }
        public Room? Room { get; set; }
    }
}
