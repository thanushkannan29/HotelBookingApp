using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace HotelBookingAppWebApi.Models
{
    public class Hotel
    {
        public int HotelId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        //This Collection means Hotel have many relationship with this four Tables
        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        public override string ToString()
        {
            return $"Hotel [{HotelId}] | {Name} | {City} | Contact: {ContactNumber}";
        }
    }
}
