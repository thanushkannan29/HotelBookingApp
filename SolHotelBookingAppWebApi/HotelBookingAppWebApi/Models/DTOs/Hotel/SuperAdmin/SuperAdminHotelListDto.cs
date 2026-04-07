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
}
