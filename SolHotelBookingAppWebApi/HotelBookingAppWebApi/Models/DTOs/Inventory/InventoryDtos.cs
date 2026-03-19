using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Inventory
{
    public class CreateInventoryDto
    {
        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [Required]
        public int TotalInventory { get; set; }
    }

    public class UpdateInventoryDto
    {
        [Required]
        public Guid RoomTypeInventoryId { get; set; }

        [Required]
        public int TotalInventory { get; set; }
    }

    public class InventoryResponseDto
    {
        public Guid RoomTypeInventoryId { get; set; }
        public DateOnly Date { get; set; }
        public int TotalInventory { get; set; }
        public int ReservedInventory { get; set; }
        public int Available => TotalInventory - ReservedInventory;
    }
}
