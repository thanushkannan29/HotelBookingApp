using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Inventory
{
    public class CreateInventoryDto
    {
        [Required] public Guid RoomTypeId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int TotalInventory { get; set; }
    }
}
