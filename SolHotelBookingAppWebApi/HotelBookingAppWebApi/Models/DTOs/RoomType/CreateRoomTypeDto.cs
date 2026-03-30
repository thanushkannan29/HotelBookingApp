using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class CreateRoomTypeDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required] public int MaxOccupancy { get; set; }
        public List<Guid>? AmenityIds { get; set; }
        public string? ImageUrl { get; set; }
    }
}
