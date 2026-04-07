namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class PaymentIntentDto
    {
        public string? UpiId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentRef { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
    }
}
