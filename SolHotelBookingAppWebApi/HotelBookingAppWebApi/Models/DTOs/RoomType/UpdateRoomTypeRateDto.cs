using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class UpdateRoomTypeRateDto
    {
        [Required] public Guid RoomTypeRateId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Rate { get; set; }
    }
}
