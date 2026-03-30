namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class PagedRoomTypeResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<RoomTypeListDto> RoomTypes { get; set; } = new List<RoomTypeListDto>();
    }
}
