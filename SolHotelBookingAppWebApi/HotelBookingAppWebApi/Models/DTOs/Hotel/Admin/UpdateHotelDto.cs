namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Admin
{
    public class UpdateHotelDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? UpiId { get; set; }
    }
}
