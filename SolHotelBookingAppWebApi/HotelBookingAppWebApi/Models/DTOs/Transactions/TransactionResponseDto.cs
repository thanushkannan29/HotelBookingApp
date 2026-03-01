namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class TransactionResponseDto
    {
        public Guid TransactionId { get; set; }
        public Guid ReservationId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
