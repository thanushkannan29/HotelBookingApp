namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class GetHotelReviewsRequestDto
    {
        public Guid HotelId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

}
