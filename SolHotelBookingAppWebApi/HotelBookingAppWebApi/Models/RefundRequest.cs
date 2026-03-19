using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    /// <summary>
    /// Tracks refund requests initiated by guests upon cancellation.
    /// Admin must approve before the actual financial refund is processed.
    /// </summary>
    public class RefundRequest
    {
        [Key]
        public Guid RefundRequestId { get; set; }

        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public RefundRequestStatus Status { get; set; } = RefundRequestStatus.Pending;

        /// <summary>Admin's response/note when approving or rejecting</summary>
        public string? AdminResponse { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public Reservation? Reservation { get; set; }
        public User? User { get; set; }
    }

    public enum RefundRequestStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}
