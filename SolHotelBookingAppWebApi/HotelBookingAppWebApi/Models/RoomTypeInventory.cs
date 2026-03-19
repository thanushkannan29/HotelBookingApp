using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingAppWebApi.Models
{
    public class RoomTypeInventory
    {
        [Key]
        public Guid RoomTypeInventoryId { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public int TotalInventory { get; set; }

        [Required]
        public int ReservedInventory { get; set; }

        [NotMapped]
        public int AvailableInventory => TotalInventory - ReservedInventory;

        public RoomType? RoomType { get; set; }
    }
}
