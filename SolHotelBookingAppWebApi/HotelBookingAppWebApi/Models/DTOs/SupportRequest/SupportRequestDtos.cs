using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.SupportRequest
{
    // ── Submission DTOs ───────────────────────────────────────────────────────

    /// <summary>Used by unauthenticated (public) visitors</summary>
    public class PublicSupportRequestDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>Used by authenticated guests — can reference a reservation</summary>
    public class GuestSupportRequestDto
    {
        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>e.g. "Complaint", "Billing", "Refund", "Other"</summary>
        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ReservationCode { get; set; }

        public Guid? HotelId { get; set; }
    }

    /// <summary>Used by hotel admins — bug/issue reports</summary>
    public class AdminSupportRequestDto
    {
        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>e.g. "Bug", "Feature Request", "Dashboard Issue", "Payment Issue", "Other"</summary>
        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;
    }

    // ── Response DTO ──────────────────────────────────────────────────────────

    public class SupportRequestResponseDto
    {
        public Guid SupportRequestId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AdminResponse { get; set; }
        public string SubmitterRole { get; set; } = string.Empty;
        public string SubmitterName { get; set; } = string.Empty;
        public string SubmitterEmail { get; set; } = string.Empty;
        public string? ReservationCode { get; set; }
        public string? HotelName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class PagedSupportRequestResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<SupportRequestResponseDto> Requests { get; set; } = new List<SupportRequestResponseDto>();
    }

    // ── SuperAdmin respond ────────────────────────────────────────────────────

    public class RespondSupportRequestDto
    {
        [Required, MaxLength(2000)]
        public string Response { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Resolved";
    }
}
