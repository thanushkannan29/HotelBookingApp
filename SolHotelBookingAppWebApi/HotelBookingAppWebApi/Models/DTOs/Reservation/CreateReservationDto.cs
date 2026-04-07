using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Reservation
{
    /// <summary>Payload for creating a new reservation.</summary>
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

        /// <summary>Optional: guest can explicitly select room IDs; if empty, system auto-assigns.</summary>
        public List<Guid>? SelectedRoomIds { get; set; }

        /// <summary>Optional promo code to apply a discount.</summary>
        public string? PromoCodeUsed { get; set; }

        /// <summary>Amount from wallet to deduct (0 = no wallet payment).</summary>
        public decimal WalletAmountToUse { get; set; } = 0;

        /// <summary>Guest opts in to pay the 10% cancellation protection fee.</summary>
        public bool PayCancellationFee { get; set; } = false;
    }
}
