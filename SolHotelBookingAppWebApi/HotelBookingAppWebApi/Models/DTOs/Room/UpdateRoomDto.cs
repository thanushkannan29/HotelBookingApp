using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Room
{
    public class UpdateRoomDto
    {
        [Required] public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public Guid RoomTypeId { get; set; }
    }
}
