namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class GetHotelReviewsRequestDto
    {
        public Guid HotelId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
        public string? SortDir { get; set; }
    }
}
