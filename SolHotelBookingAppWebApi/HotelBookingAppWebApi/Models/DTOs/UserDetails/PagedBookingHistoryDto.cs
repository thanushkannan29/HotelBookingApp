namespace HotelBookingAppWebApi.Models.DTOs.UserDetails
{
    public class PagedBookingHistoryDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<BookingHistoryDto> Bookings { get; set; } = new List<BookingHistoryDto>();
    }
}
