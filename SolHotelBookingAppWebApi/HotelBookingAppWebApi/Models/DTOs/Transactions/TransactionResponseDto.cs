using HotelBookingAppWebApi.Models;
namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class TransactionResponseDto
    {
        public Guid TransactionId { get; set; }
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = "Payment";
        public string? Description { get; set; }
    }
}
