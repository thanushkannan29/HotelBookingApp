namespace HotelBookingAppWebApi.Models.DTOs.SupportRequest
{
    public class PagedSupportRequestResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<SupportRequestResponseDto> Requests { get; set; } = new List<SupportRequestResponseDto>();
    }
}
