namespace HotelBookingAppWebApi.Models
{
    public class ReservationRoom
    {
        public int ReservationRoomId { get; set; }

        public int ReservationId { get; set; }
        public Reservation? Reservation { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }

        public decimal PricePerNight { get; set; }
        public override string ToString()
        {
            return $"ReservationRoom [{ReservationRoomId}] | ReservationId: {ReservationId} | RoomId: {RoomId} | Price/Night: {PricePerNight:C}";
        }

    }
}
