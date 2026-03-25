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

    public class HotelDetailsDto
    {
        public Guid HotelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public IEnumerable<string> Amenities { get; set; } = new List<string>();
        public IEnumerable<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();
        public IEnumerable<RoomTypePublicDto> RoomTypes { get; set; } = new List<RoomTypePublicDto>();
    }

    public class ReviewDto
    {
        public string UserName { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class RoomTypePublicDto
    {
        public Guid RoomTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxOccupancy { get; set; }
        public IEnumerable<string> Amenities { get; set; } = new List<string>();

        /// <summary>Optional room type photo URL</summary>
        public string? ImageUrl { get; set; }
    }

    public class RoomAvailabilityDto
    {
        public Guid RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int AvailableRooms { get; set; }

        /// <summary>Optional room type photo URL</summary>
        public string? ImageUrl { get; set; }
    }

    public class SearchHotelRequestDto
    {
        public string City { get; set; } = string.Empty;
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SearchHotelResponseDto
    {
        public IEnumerable<HotelListItemDto> Hotels { get; set; } = new List<HotelListItemDto>();
        public int PageNumber { get; set; }
        public int RecordsCount { get; set; }
    }
}