using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class CreateRoomTypeRateDto
    {
        [Required] public Guid RoomTypeId { get; set; }
        [Required] public DateOnly StartDate { get; set; }
        [Required] public DateOnly EndDate { get; set; }
        [Required] public decimal Rate { get; set; }
    }
}
