using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class SuperAdminRevenue
    {
        [Key]
        public Guid SuperAdminRevenueId { get; set; }

        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public Guid HotelId { get; set; }

        public decimal ReservationAmount { get; set; }

        public decimal CommissionAmount { get; set; } // 2% of ReservationAmount

        [MaxLength(100)]
        public string SuperAdminUpiId { get; set; } = "thanushstayhubsuperadmin@okaxis";

        public DateTime CreatedAt { get; set; }

        public Reservation? Reservation { get; set; }
        public Hotel? Hotel { get; set; }
    }
}
