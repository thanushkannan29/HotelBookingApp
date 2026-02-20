namespace HotelBookingAppWebApi.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }

        public int HotelId { get; set; }
        public Hotel? Hotel { get; set; }

        public int RoomTypeId { get; set; }
        public RoomType? RoomType { get; set; }

        public ICollection<ReservationRoom> ReservationRooms { get; set; } = new List<ReservationRoom>();

        public override string ToString()
        {
            return $"Room [{RoomId}] | Number: {RoomNumber} | Floor: {Floor} | TypeId: {RoomTypeId} | HotelId: {HotelId}";
        }

    }
}
