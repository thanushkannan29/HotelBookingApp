namespace HotelBookingAppWebApi.Models.DTOs.Wallet
{
    /// <summary>Current wallet balance and last updated timestamp.</summary>
    public class WalletResponseDto
    {
        public Guid WalletId { get; set; }
        public decimal Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
