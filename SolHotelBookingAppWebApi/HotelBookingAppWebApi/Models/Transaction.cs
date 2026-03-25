using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Transaction
    {
        [Key]
        public Guid TransactionId { get; set; }

        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        public bool WalletUsed { get; set; } = false;
        public decimal WalletAmountUsed { get; set; } = 0;

        public Reservation? Reservation { get; set; }
    }

    public enum PaymentMethod
    {
        CreditCard = 1,
        DebitCard = 2,
        UPI = 3,
        NetBanking = 4,
        Wallet = 5
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Success = 2,
        Failed = 3,
        Refunded = 4
    }
}
