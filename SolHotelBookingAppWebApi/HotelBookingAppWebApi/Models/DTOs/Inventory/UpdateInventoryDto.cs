using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Inventory
{
    public class UpdateInventoryDto
    {
        
        [Required]
        public Guid RoomTypeInventoryId { get; set; }

        [Required]
        public int TotalInventory { get; set; }
    }
}
