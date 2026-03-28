using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }
    }

    public class RefundRequestDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class TransactionResponseDto
    {
        public Guid TransactionId { get; set; }
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime TransactionDate { get; set; }
    }

    public class PagedTransactionResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<TransactionResponseDto> Transactions { get; set; } = new List<TransactionResponseDto>();
    }

    /// <summary>Returned by GET /api/transactions/payment-intent/{reservationId}</summary>
    public class PaymentIntentDto
    {
        public string? UpiId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentRef { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
    }
}