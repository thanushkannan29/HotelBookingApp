using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Inventory
{
    public class AdjustReservedInventoryDto
    {
        
        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public int Quantity { get; set; } // positive or negative
    }
}
