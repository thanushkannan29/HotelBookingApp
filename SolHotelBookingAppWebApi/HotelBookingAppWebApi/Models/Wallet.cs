using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Wallet
    {
        [Key]
        public Guid WalletId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public decimal Balance { get; set; } = 0;

        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
        public ICollection<WalletTransaction>? WalletTransactions { get; set; }
    }

    public class WalletTransaction
    {
        [Key]
        public Guid WalletTransactionId { get; set; }

        [Required]
        public Guid WalletId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required, MaxLength(10)]
        public string Type { get; set; } = string.Empty; // "Credit" | "Debit"

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Wallet? Wallet { get; set; }
    }
}
