namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class MyReviewsResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid HotelId { get; set; }

        public string HotelName { get; set; } = string.Empty;

        public decimal Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }
    }
}
