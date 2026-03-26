using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    public class CreateReservationDto
    {
        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateOnly CheckInDate { get; set; }

        [Required]
        public DateOnly CheckOutDate { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Number of rooms must be at least 1")]
        public int NumberOfRooms { get; set; }

        /// <summary>Optional: guest can explicitly select room IDs; if empty, system auto-assigns</summary>
        public List<Guid>? SelectedRoomIds { get; set; }

        /// <summary>Optional promo code to apply discount</summary>
        public string? PromoCodeUsed { get; set; }

        /// <summary>Amount from wallet to deduct (0 = no wallet payment)</summary>
        public decimal WalletAmountToUse { get; set; } = 0;
    }

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

    public class RoomSummaryDto
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
    }

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
        public List<RoomSummaryDto> Rooms { get; set; } = new();
    }

    public class PagedReservationResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<ReservationDetailsDto> Reservations { get; set; } = new List<ReservationDetailsDto>();
    }

    public class CancelReservationDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class AvailableRoomDto
    {
        public Guid RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
    }

    public class QrPaymentResponseDto
    {
        public string UpiId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string QrCodeBase64 { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
    }
}
