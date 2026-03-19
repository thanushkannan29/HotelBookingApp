using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.UserDetails
{
    public class UserProfileResponseDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserProfileDto
    {
        [MaxLength(150)]
        public string? Name { get; set; }

        [MaxLength(15)]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Pincode { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    public class BookingHistoryDto
    {
        public Guid ReservationId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class PagedBookingHistoryDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<BookingHistoryDto> Bookings { get; set; } = new List<BookingHistoryDto>();
    }

    public class PaginationDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
    }
}
