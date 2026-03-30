namespace HotelBookingAppWebApi.Models.DTOs.Amenity
{
    /// <summary>Paged wrapper for amenity list responses.</summary>
    public class PagedAmenityResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<AmenityResponseDto> Amenities { get; set; } = new List<AmenityResponseDto>();
    }
}
