namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class HotelDetailsDto
    {
        public Guid HotelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? UpiId { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public decimal GstPercent { get; set; }
        public IEnumerable<string> Amenities { get; set; } = new List<string>();
        public IEnumerable<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();
        public IEnumerable<RoomTypePublicDto> RoomTypes { get; set; } = new List<RoomTypePublicDto>();
    }
}
