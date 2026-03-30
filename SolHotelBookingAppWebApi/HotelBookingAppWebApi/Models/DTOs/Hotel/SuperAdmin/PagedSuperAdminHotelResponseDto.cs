namespace HotelBookingAppWebApi.Models.DTOs.Hotel.SuperAdmin
{
    public class PagedSuperAdminHotelResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<SuperAdminHotelListDto> Hotels { get; set; } = new List<SuperAdminHotelListDto>();
    }
}
