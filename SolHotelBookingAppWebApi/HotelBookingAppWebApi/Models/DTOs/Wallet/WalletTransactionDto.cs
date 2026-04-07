namespace HotelBookingAppWebApi.Models.DTOs.Wallet
{
    /// <summary>Single wallet transaction entry (credit or debit).</summary>
    public class WalletTransactionDto
    {
        public Guid WalletTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
