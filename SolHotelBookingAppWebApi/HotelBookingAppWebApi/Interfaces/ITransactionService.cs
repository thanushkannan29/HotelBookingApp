using HotelBookingAppWebApi.Models.DTOs.Transactions;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Payment creation, refunds, and transaction history across all roles.</summary>
    public interface ITransactionService
    {
        /// <summary>Records a successful payment and promotes the reservation to Confirmed.</summary>
        Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto);

        /// <summary>
        /// Guest-only direct refund within 30 minutes of payment.
        /// After 30 minutes this throws <c>PaymentException</c>.
        /// </summary>
        Task<TransactionResponseDto> DirectGuestRefundAsync(
            Guid transactionId, Guid userId, RefundRequestDto dto);

        /// <summary>
        /// Returns paged transaction history.
        /// Guest sees own; Admin sees hotel's (all statuses); SuperAdmin sees all.
        /// </summary>
        Task<PagedTransactionResponseDto> GetAllTransactionsAsync(
            Guid userId, string role, int page, int pageSize,
            string? sortField = null, string? sortDir = null);

        /// <summary>Returns the hotel's UPI ID and payment reference so the guest can pay externally.</summary>
        Task<PaymentIntentDto> GetPaymentIntentAsync(Guid reservationId, Guid userId);

        /// <summary>Admin marks a successful transaction as Failed and resets the reservation to Pending.</summary>
        Task MarkTransactionFailedAsync(Guid transactionId, Guid adminUserId);

        /// <summary>Records a failed payment attempt for audit purposes.</summary>
        Task RecordFailedPaymentAsync(Guid reservationId, Guid userId);
    }
}
