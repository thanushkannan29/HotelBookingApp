namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class HotelListItemDto
    {
        public Guid HotelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal? StartingPrice { get; set; }
    }
}
