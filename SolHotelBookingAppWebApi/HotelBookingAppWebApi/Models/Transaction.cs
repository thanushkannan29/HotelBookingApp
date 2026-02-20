namespace HotelBookingAppWebApi.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }

        public int ReservationId { get; set; }
        public Reservation? Reservation { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"Transaction [{TransactionId}] | ReservationId: {ReservationId} | " +
                   $"Amount: {Amount:C} | Method: {PaymentMethod} | Status: {Status} | Date: {TransactionDate:yyyy-MM-dd HH:mm}";
        }

    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        UPI,
        NetBanking,
        Wallet
    }

    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Refunded
    }
}
