namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>Full reservation details including pricing breakdown and room list.</summary>
    public class ReservationDetailsDto
    {
        public string ReservationCode { get; set; } = string.Empty;
        public Guid ReservationId { get; set; }
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public Guid RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public int NumberOfRooms { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal WalletAmountUsed { get; set; }
        public decimal FinalAmount { get; set; }
        public string? PromoCodeUsed { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsCheckedIn { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryTime { get; set; }
        public string? UpiId { get; set; }
        public bool CancellationFeePaid { get; set; }
        public decimal CancellationFeeAmount { get; set; }
        public string CancellationPolicyText { get; set; } = string.Empty;
        public List<RoomSummaryDto> Rooms { get; set; } = new();
    }
}
