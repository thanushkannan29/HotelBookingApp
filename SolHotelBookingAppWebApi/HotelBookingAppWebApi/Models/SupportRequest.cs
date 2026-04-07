using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    /// <summary>
    /// Support / complaint requests submitted via the Contact page.
    /// - Unauthenticated users: name + email + message (no UserId/HotelId)
    /// - Guests: linked to UserId, can reference a completed reservation
    /// - Admins: linked to UserId + HotelId, bug/issue reports
    /// SuperAdmin can respond; response is visible to the submitter.
    /// </summary>
    public class SupportRequest
    {
        [Key]
        public Guid SupportRequestId { get; set; }

        // ── Submitter info ────────────────────────────────────────────────────
        /// <summary>Null for unauthenticated submissions</summary>
        public Guid? UserId { get; set; }

        /// <summary>Role of submitter: Guest, Admin, or null for public</summary>
        [MaxLength(20)]
        public string? SubmitterRole { get; set; }

        /// <summary>For unauthenticated users — their provided name</summary>
        [MaxLength(150)]
        public string? GuestName { get; set; }

        /// <summary>For unauthenticated users — their provided email</summary>
        [MaxLength(200)]
        public string? GuestEmail { get; set; }

        // ── Request details ───────────────────────────────────────────────────
        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>Guest: complaint about a hotel; Admin: bug report category</summary>
        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>For guest complaints — the reservation code they're referencing</summary>
        [MaxLength(50)]
        public string? ReservationCode { get; set; }

        /// <summary>For guest complaints — the hotel they're complaining about</summary>
        public Guid? HotelId { get; set; }

        // ── Status & response ─────────────────────────────────────────────────
        public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Open;

        [MaxLength(2000)]
        public string? AdminResponse { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        public User? User { get; set; }
        public Hotel? Hotel { get; set; }
    }

    public enum SupportRequestStatus
    {
        Open = 1,
        InProgress = 2,
        Resolved = 3
    }
}
