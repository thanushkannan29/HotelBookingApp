namespace HotelBookingAppWebApi.Models
{
    public class RoomTypeAmenity
    {
        public Guid RoomTypeId { get; set; }
        public Guid AmenityId { get; set; }
        public RoomType? RoomType { get; set; }
        public Amenity? Amenity { get; set; }
    }
}
