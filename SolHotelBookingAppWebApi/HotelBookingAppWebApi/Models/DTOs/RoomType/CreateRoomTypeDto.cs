using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class CreateRoomTypeDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public int MaxOccupancy { get; set; }

        public string Amenities { get; set; } = string.Empty;
    }
}
