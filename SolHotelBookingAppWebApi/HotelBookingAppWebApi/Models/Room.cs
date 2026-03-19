using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Room
    {
        [Key]
        public Guid RoomId { get; set; }

        [Required]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        public int Floor { get; set; }

        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }

        public bool IsActive { get; set; } = true;

        public Hotel? Hotel { get; set; }
        public RoomType? RoomType { get; set; }

        public ICollection<ReservationRoom>? ReservationRooms { get; set; }
    }
}
