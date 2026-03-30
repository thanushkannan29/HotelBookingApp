namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class ReviewDto
    {
        public string UserName { get; set; } = string.Empty;
        public string? UserProfileImageUrl { get; set; }
        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? AdminReply { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
