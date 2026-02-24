namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class ReviewDto
    {
        public string UserName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
