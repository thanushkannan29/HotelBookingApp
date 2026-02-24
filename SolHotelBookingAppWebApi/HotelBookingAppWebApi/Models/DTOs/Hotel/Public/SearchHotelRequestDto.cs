namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class SearchHotelRequestDto
    {
        public string City { get; set; } = string.Empty;
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
