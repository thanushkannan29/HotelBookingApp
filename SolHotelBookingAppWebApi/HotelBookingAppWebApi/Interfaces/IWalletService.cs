using HotelBookingAppWebApi.Models.DTOs.Wallet;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Guest wallet operations — balance management and transaction history.</summary>
    public interface IWalletService
    {
        /// <summary>Returns paged wallet transaction history and current balance.</summary>
        Task<PagedWalletTransactionDto> GetWalletAsync(Guid userId, int page, int pageSize);

        /// <summary>Adds funds to the wallet. Throws if amount is not positive.</summary>
        Task<WalletResponseDto> TopUpAsync(Guid userId, decimal amount);

        /// <summary>Admin view of a guest's wallet balance. Requires Admin role.</summary>
        Task<WalletResponseDto> GetGuestWalletByAdminAsync(Guid adminUserId, Guid guestUserId);

        /// <summary>Credits the wallet. Caller is responsible for committing the outer transaction.</summary>
        Task CreditAsync(Guid userId, decimal amount, string description);

        /// <summary>Debits the wallet. Returns <c>false</c> if balance is insufficient.</summary>
        Task<bool> DeductAsync(Guid userId, decimal amount, string description);

        /// <summary>Debits up to the available balance — never throws on insufficient funds.</summary>
        Task<bool> DebitAsync(Guid userId, decimal amount, string description);

        /// <summary>Creates a zero-balance wallet if one does not already exist for the user.</summary>
        Task EnsureWalletExistsAsync(Guid userId);
    }
}
