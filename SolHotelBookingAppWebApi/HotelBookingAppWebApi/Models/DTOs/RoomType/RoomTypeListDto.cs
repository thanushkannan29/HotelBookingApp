namespace HotelBookingAppWebApi.Models.DTOs.RoomType
{
    public class RoomTypeListDto
    {
        public Guid RoomTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MaxOccupancy { get; set; }
        public List<AmenityItemDto> AmenityList { get; set; } = new();
        public bool IsActive { get; set; }
        public int RoomCount { get; set; }
        public string? ImageUrl { get; set; }
    }
}
