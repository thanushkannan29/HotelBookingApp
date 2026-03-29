using HotelBookingAppWebApi.Models.DTOs.Transactions;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto);

        /// <summary>
        /// Guest-only direct refund within 30 minutes of payment.
        /// After 30 min this throws. Admin refunds use RefundRequestService.
        /// </summary>
        Task<TransactionResponseDto> DirectGuestRefundAsync(Guid transactionId, Guid userId, RefundRequestDto dto);

        Task<PagedTransactionResponseDto> GetAllTransactionsAsync(Guid userId, string role, int page, int pageSize, string? sortField = null, string? sortDir = null);

        /// <summary>Returns UPI ID and payment reference for a reservation so the guest can pay via UPI</summary>
        Task<PaymentIntentDto> GetPaymentIntentAsync(Guid reservationId, Guid userId);

        /// <summary>Admin marks a transaction as Failed and resets reservation to Pending so guest can retry</summary>
        Task MarkTransactionFailedAsync(Guid transactionId, Guid adminUserId);

        /// <summary>Records a failed payment attempt (e.g. Razorpay failure) as a Failed transaction</summary>
        Task RecordFailedPaymentAsync(Guid reservationId, Guid userId);
    }
}