using HotelBookingAppWebApi.Models.DTOs.Transactions;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto);
        Task<TransactionResponseDto> RefundAsync(Guid transactionId, RefundRequestDto dto);
        Task<PagedTransactionResponseDto> GetAllTransactionsAsync(Guid userId, string role, int page, int pageSize);
    }
}
