namespace HotelBookingAppWebApi.Models.DTOs.Review
{
    public class ReviewResponseDto
    {
        public Guid ReviewId { get; set; }
        public Guid HotelId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? UserProfileImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AdminReply { get; set; }
        public int ContributionPoints { get; set; } = 100;
    }
}
