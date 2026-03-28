namespace HotelBookingAppWebApi.Models.DTOs.Hotel.Admin
{
    public class UpdateHotelDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? UpiId { get; set; }
    }

    public class UpdateHotelGstDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(0, 28, ErrorMessage = "GST must be between 0 and 28")]
        public decimal GstPercent { get; set; }
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