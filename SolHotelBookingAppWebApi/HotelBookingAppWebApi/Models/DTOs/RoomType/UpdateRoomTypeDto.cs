using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.RoomType

{
    public class UpdateRoomTypeDto
    {
        [Required]
        public Guid RoomTypeId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxOccupancy { get; set; }
        public string Amenities { get; set; } = string.Empty;
    }
}
