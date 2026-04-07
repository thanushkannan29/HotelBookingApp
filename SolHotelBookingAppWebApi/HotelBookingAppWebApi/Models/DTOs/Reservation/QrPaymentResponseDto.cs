namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>UPI QR code payload returned for a pending reservation payment.</summary>
    public class QrPaymentResponseDto
    {
        public string UpiId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string QrCodeBase64 { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
    }
}
