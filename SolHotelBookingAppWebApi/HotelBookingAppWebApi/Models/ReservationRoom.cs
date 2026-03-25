using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>
        /// Computed: true if this room has an active Confirmed reservation covering today.
        /// Not mapped to DB — used for in-memory occupancy checks only.
        /// </summary>
        [NotMapped]
        public bool IsCurrentlyOccupied =>
            Reservation != null &&
            Reservation.Status == ReservationStatus.Confirmed &&
            Reservation.CheckInDate <= DateOnly.FromDateTime(DateTime.UtcNow) &&
            Reservation.CheckOutDate > DateOnly.FromDateTime(DateTime.UtcNow);
    }
}