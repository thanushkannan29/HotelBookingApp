namespace HotelBookingAppWebApi.Models.DTOs.Inventory
{
    public class InventoryResponseDto
    {
        public Guid RoomTypeInventoryId { get; set; }
        public DateOnly Date { get; set; }
        public int TotalInventory { get; set; }
        public int ReservedInventory { get; set; }
        public int Available { get; set; }
    }
}
