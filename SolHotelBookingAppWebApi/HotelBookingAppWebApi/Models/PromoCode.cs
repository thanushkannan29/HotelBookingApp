using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class PromoCode
    {
        [Key]
        public Guid PromoCodeId { get; set; }

        [Required, MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public Guid ReservationId { get; set; }

        public decimal DiscountPercent { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
        public Hotel? Hotel { get; set; }
        public Reservation? Reservation { get; set; }
    }
}
