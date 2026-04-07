namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>Returned immediately after a reservation is created.</summary>
    public class ReservationResponseDto
    {
        public string ReservationCode { get; set; } = string.Empty;
        public Guid ReservationId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal WalletAmountUsed { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public List<RoomSummaryDto> Rooms { get; set; } = new();
    }
}
