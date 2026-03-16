namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Public
{
    public class RoomTypePublicDto
    {
        public Guid RoomTypeId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int MaxOccupancy { get; set; }

        public IEnumerable<string> Amenities { get; set; } = new List<string>();
    }
}
