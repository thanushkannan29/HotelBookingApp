namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class RoomTypeRateDto
    {
        public Guid RoomTypeRateId { get; set; }
        public Guid RoomTypeId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Rate { get; set; }
    }
}
