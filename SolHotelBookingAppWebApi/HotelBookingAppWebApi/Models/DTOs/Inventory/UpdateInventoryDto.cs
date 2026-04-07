using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Inventory
{
    public class UpdateInventoryDto
    {
        [Required] public Guid RoomTypeInventoryId { get; set; }
        public int TotalInventory { get; set; }
    }
}
