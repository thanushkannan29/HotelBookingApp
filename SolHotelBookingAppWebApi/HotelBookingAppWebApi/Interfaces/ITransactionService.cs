using HotelBookingAppWebApi.Models.DTOs.Transactions;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponseDto> CreatePaymentAsync(CreatePaymentDto dto);
        Task<TransactionResponseDto> UpdatePaymentStatusAsync(Guid transactionId, UpdatePaymentStatusDto dto);
        Task<TransactionResponseDto> RefundAsync(Guid transactionId, RefundRequestDto dto);
        Task<PagedTransactionResponseDto> GetAllTransactionsAsync(int page, int pageSize);
    }
}
