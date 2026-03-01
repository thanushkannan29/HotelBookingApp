namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class HotelDetailsDto
    {
        public Guid HotelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal AverageRating { get; set; }

        public IEnumerable<string> Amenities { get; set; } = new List<string>();

        public IEnumerable<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();
    }
}
