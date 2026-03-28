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

        /// <summary>FK to Reservations � one review per completed reservation</summary>
        [Required]
        public Guid ReservationId { get; set; }

        [Range(1, 5)]
        public decimal Rating { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        /// <summary>Optional review image URL</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Hotel admin reply to this review</summary>
        public string? AdminReply { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public User? User { get; set; }
        public Hotel? Hotel { get; set; }
        public Reservation? Reservation { get; set; }
    }
}