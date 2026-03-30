namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class MyReviewsResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ContributionPoints { get; set; } = 100;
    }
}
