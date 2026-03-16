using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models
{
    public class Reservation
    {
        [Key]
        public Guid ReservationId { get; set; }

        [Required]
        public string ReservationCode { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public DateOnly CheckInDate { get; set; }
        [Required]
        public DateOnly CheckOutDate { get; set; }


        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public ReservationStatus Status { get; set; }

        public DateTime? CancelledDate { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? ExpiryTime { get; set; }


        [Required]
        public DateTime CreatedDate { get; set; }

        public User? User { get; set; }
        public Hotel? Hotel { get; set; }

        public ICollection<ReservationRoom>? ReservationRooms { get; set; }
        public ICollection<Transaction>? Transactions { get; set; }
    }

    public enum ReservationStatus
    {
        Pending = 1,
        Confirmed = 2,
        Cancelled = 3,
        Completed = 4
    }
}
