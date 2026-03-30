namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class SearchHotelResponseDto
    {
        public IEnumerable<HotelListItemDto> Hotels { get; set; } = new List<HotelListItemDto>();
        public int PageNumber { get; set; }
        public int RecordsCount { get; set; }
        public int TotalCount { get; set; }
    }
}
