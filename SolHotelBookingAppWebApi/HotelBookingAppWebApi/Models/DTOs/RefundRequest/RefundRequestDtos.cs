using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.RefundRequest
{
    public class RefundRequestResponseDto
    {
        public Guid RefundRequestId { get; set; }
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AdminResponse { get; set; }
        public decimal RefundAmount { get; set; }
        public string? RefundNote { get; set; }

        /// <summary>How the refund was paid back e.g. 'UPI', 'Bank Transfer', 'Cash'</summary>
        public string? RefundPaymentMethod { get; set; }

        /// <summary>Admin-entered reference number for the refund payment</summary>
        public string? RefundTransactionRef { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class ProcessRefundDto
    {
        [Required]
        public string AdminResponse { get; set; } = string.Empty;

        /// <summary>How the refund was paid back e.g. 'UPI', 'Bank Transfer', 'Cash'</summary>
        public string? RefundPaymentMethod { get; set; }

        /// <summary>Admin-entered reference number for the refund payment</summary>
        public string? RefundTransactionRef { get; set; }
    }

    public class PagedRefundRequestResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<RefundRequestResponseDto> RefundRequests { get; set; } = new List<RefundRequestResponseDto>();
    }
}