using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Room
{
    public class CreateRoomDto
    {
        [Required] public string RoomNumber { get; set; } = string.Empty;
        [Required] public int Floor { get; set; }
        [Required] public Guid RoomTypeId { get; set; }
    }
}
