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
}
