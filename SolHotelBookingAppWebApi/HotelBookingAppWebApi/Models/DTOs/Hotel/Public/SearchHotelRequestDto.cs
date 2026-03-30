namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class SearchHotelRequestDto
    {
        public string? City { get; set; }
        public string? State { get; set; }
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<Guid>? AmenityIds { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? RoomType { get; set; }
        public string? SortBy { get; set; }
    }
}
