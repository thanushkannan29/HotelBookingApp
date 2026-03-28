using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Hotel
    {
        [Key]
        public Guid HotelId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string ContactNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>SuperAdmin can block a hotel, preventing the Admin from activating it</summary>
        public bool IsBlockedBySuperAdmin { get; set; } = false;

        /// <summary>UPI ID for simulated payment flow e.g. 'hotel@upi'</summary>
        [MaxLength(50)]
        public string? UpiId { get; set; }

        /// <summary>GST percentage set by hotel admin (0–28)</summary>
        public decimal GstPercent { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; }

        public ICollection<RoomType>? RoomTypes { get; set; }
        public ICollection<Room>? Rooms { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Reservation>? Reservations { get; set; }
    }
}