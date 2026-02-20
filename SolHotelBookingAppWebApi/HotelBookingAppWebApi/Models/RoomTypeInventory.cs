namespace HotelBookingAppWebApi.Models
{
    public class RoomTypeInventory
    {
        public int RoomTypeInventoryId { get; set; }

        public int RoomTypeId { get; set; }
        public RoomType? RoomType { get; set; }

        public DateOnly Date { get; set; }

        public int TotalInventory { get; set; }
        public int ReservedInventory { get; set; }

        public override string ToString()
        {
            return $"Inventory [{RoomTypeInventoryId}] | RoomTypeId: {RoomTypeId} | Date: {Date:yyyy-MM-dd} | " +
                   $"Total: {TotalInventory} | Reserved: {ReservedInventory}";
        }

    }
}
