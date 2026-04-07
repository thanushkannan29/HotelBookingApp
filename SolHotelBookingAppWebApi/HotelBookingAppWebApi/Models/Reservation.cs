using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Reservation
    {
        [Key]
        public Guid ReservationId { get; set; }

        [Required]
        public string ReservationCode { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public DateOnly CheckInDate { get; set; }

        [Required]
        public DateOnly CheckOutDate { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public ReservationStatus Status { get; set; }

        /// <summary>Tracks whether the guest physically checked in</summary>
        public bool IsCheckedIn { get; set; } = false;

        public DateTime? CancelledDate { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? ExpiryTime { get; set; }

        // GST & Promo fields
        public decimal GstPercent { get; set; } = 0;
        public decimal GstAmount { get; set; } = 0;
        public decimal DiscountPercent { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal WalletAmountUsed { get; set; } = 0;
        public string? PromoCodeUsed { get; set; }
        public decimal FinalAmount { get; set; } = 0;

        [Required]
        public DateTime CreatedDate { get; set; }

        /// <summary>Whether the guest paid the 10% cancellation protection fee at booking time</summary>
        public bool CancellationFeePaid { get; set; } = false;

        /// <summary>Actual cancellation protection fee amount paid (10% of TotalAmount)</summary>
        public decimal CancellationFeeAmount { get; set; } = 0;

        public User? User { get; set; }
        public Hotel? Hotel { get; set; }

        public ICollection<ReservationRoom>? ReservationRooms { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }

    public enum ReservationStatus
    {
        Pending = 1,
        Confirmed = 2,
        Cancelled = 3,
        Completed = 4,
        NoShow = 5
    }
}
