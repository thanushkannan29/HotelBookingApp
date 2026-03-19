using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    /// <summary>
    /// Stores audit trail for critical entity changes such as Hotel/Room/RoomType updates
    /// and refund approvals/rejections.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public Guid AuditLogId { get; set; }

        public Guid? UserId { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;  // e.g. "HotelUpdated", "RefundApproved"

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty; // e.g. "Hotel", "Reservation"

        public Guid? EntityId { get; set; }

        /// <summary>JSON string describing what changed</summary>
        public string Changes { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
