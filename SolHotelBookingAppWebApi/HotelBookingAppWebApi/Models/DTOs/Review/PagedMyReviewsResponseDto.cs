namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class PagedMyReviewsResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<MyReviewsResponseDto> Reviews { get; set; } = new List<MyReviewsResponseDto>();
    }
}
