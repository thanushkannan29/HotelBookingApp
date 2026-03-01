using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class ReservationRoom
    {
        public Guid ReservationRoomId { get; set; }

        public Guid ReservationId { get; set; }

        public Guid RoomTypeId { get; set; }
        public int NumberOfRooms { get; set; }

        public decimal PricePerNight { get; set; }

        public Reservation? Reservation { get; set; }
        public RoomType? RoomType { get; set; }
    }


}
