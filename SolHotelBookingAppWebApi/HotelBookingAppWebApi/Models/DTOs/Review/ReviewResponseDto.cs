namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class ReviewResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid HotelId { get; set; }
        public Guid UserId { get; set; }
        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
