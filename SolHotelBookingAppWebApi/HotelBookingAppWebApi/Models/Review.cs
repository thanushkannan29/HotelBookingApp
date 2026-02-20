namespace HotelBookingAppWebApi.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int HotelId { get; set; }
        public Hotel? Hotel { get; set; }

        public int Rating { get; set; }   // 1–5
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"Review [{ReviewId}] | HotelId: {HotelId} | UserId: {UserId} | Rating: {Rating}/5 | Date: {CreatedDate:yyyy-MM-dd}";
        }

    }
}
