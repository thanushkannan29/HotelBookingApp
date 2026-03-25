using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Wallet
{
    public class WalletResponseDto
    {
        public Guid WalletId { get; set; }
        public decimal Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WalletTransactionDto
    {
        public Guid WalletTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PagedWalletTransactionDto
    {
        public int TotalCount { get; set; }
        public WalletResponseDto Wallet { get; set; } = new();
        public IEnumerable<WalletTransactionDto> Transactions { get; set; } = new List<WalletTransactionDto>();
    }

    public class TopUpWalletDto
    {
        [Required, Range(1, 100000)]
        public decimal Amount { get; set; }
    }
}
