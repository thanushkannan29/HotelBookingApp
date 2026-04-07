namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class GetRateByDateRequestDto
    {
        public Guid RoomTypeId { get; set; }
        public DateOnly Date { get; set; }
    }
}
