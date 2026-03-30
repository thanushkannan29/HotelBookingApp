namespace HotelBookingAppWebApi.Models.DTOs.Wallet
{
    /// <summary>Paged wallet transaction history with current balance.</summary>
    public class PagedWalletTransactionDto
    {
        public int TotalCount { get; set; }
        public WalletResponseDto Wallet { get; set; } = new();
        public IEnumerable<WalletTransactionDto> Transactions { get; set; } = new List<WalletTransactionDto>();
    }
}
