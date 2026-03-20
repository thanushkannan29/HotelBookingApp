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

        Task<PagedTransactionResponseDto> GetAllTransactionsAsync(Guid userId, string role, int page, int pageSize);
    }
}