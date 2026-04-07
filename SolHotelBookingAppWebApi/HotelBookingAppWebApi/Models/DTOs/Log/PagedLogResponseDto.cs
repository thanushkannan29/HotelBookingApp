namespace HotelBookingAppWebApi.Models.DTOs.Log
{
    public class PagedLogResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<LogResponseDto> Logs { get; set; } = new List<LogResponseDto>();
    }
}
