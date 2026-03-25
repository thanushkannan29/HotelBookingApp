namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Admin
{
    public class UpdateHotelDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>UPI ID for simulated payment flow e.g. 'hotel@upi'</summary>
        public string? UpiId { get; set; }
    }
}

namespace HotelBookingAppWebApi.Models.DTOs.Hotel.SuperAdmin
{
    public class SuperAdminHotelListDto
    {
        public Guid HotelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsBlockedBySuperAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalReservations { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PagedSuperAdminHotelResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<SuperAdminHotelListDto> Hotels { get; set; } = new List<SuperAdminHotelListDto>();
    }
}