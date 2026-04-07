namespace HotelBookingAppWebApi.Models.DTOs.AmenityRequest
{
    /// <summary>Paged wrapper for amenity request list responses.</summary>
    public class PagedAmenityRequestResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<AmenityRequestResponseDto> Requests { get; set; } = new List<AmenityRequestResponseDto>();
    }
}
