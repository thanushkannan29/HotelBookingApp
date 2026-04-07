namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class PagedReviewResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<ReviewResponseDto> Reviews { get; set; } = new List<ReviewResponseDto>();
    }
}
