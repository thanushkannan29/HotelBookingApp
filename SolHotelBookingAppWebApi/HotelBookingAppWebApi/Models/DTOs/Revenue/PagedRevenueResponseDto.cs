namespace HotelBookingAppWebApi.Models.DTOs.Revenue
{
    public class PagedRevenueResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<SuperAdminRevenueDto> Items { get; set; } = new List<SuperAdminRevenueDto>();
    }
}
