using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Review
    {
        [Key]
        public Guid ReviewId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid HotelId { get; set; }

        [Range(1, 5)]
        public decimal Rating { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; }

        public User? User { get; set; }
        public Hotel? Hotel { get; set; }
    }
}
