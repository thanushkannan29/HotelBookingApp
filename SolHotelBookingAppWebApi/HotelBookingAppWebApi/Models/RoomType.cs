using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class RoomType
    {
        [Key]
        public Guid RoomTypeId { get; set; }

        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public int MaxOccupancy { get; set; }

        public string Amenities { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public Hotel? Hotel { get; set; }

        public ICollection<Room>? Rooms { get; set; }
        public ICollection<RoomTypeRate>? Rates { get; set; }
        public ICollection<RoomTypeInventory>? Inventories { get; set; }
    }
}
