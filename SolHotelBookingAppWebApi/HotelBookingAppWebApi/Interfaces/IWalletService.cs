using HotelBookingAppWebApi.Models.DTOs.Wallet;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IWalletService
    {
        Task<PagedWalletTransactionDto> GetWalletAsync(Guid userId, int page, int pageSize);
        Task<WalletResponseDto> TopUpAsync(Guid userId, decimal amount);
        Task<WalletResponseDto> GetGuestWalletByAdminAsync(Guid adminUserId, Guid guestUserId);
        Task CreditAsync(Guid userId, decimal amount, string description);
        Task<bool> DeductAsync(Guid userId, decimal amount, string description);
        Task EnsureWalletExistsAsync(Guid userId);
    }
}
