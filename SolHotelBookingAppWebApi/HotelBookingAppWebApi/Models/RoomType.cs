namespace HotelBookingAppWebApi.Models
{
    public class RoomType
    {
        public int RoomTypeId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxOccupancy { get; set; }
        public string Amenities { get; set; } = string.Empty;

        public int HotelId { get; set; }
        public Hotel? Hotel { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<RoomTypeRate> Rates { get; set; } = new List<RoomTypeRate>();
        public ICollection<RoomTypeInventory> Inventories { get; set; } = new List<RoomTypeInventory>();

        public override string ToString()
        {
            return $"RoomType [{RoomTypeId}] | {Name} | Max Occupancy: {MaxOccupancy} | HotelId: {HotelId}";
        }

    }
}
