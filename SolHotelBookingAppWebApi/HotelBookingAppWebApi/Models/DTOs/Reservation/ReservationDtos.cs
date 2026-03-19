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
    }

    public class ReservationResponseDto
    {
        public string ReservationCode { get; set; } = string.Empty;
        public Guid ReservationId { get; set; }
        public decimal TotalAmount { get; set; }
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
        public string Status { get; set; } = string.Empty;
        public bool IsCheckedIn { get; set; }
        public DateTime CreatedDate { get; set; }
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
}
