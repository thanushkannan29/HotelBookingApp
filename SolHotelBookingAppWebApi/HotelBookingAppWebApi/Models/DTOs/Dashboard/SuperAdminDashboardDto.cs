namespace HotelBookingAppWebApi.Models.DTOs.Dashboard
{
    /// <summary>Platform-wide totals for the SuperAdmin dashboard.</summary>
    public class SuperAdminDashboardDto
    {
        public int TotalHotels { get; set; }
        public int ActiveHotels { get; set; }
        public int BlockedHotels { get; set; }
        public int TotalUsers { get; set; }
        public int TotalReservations { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalReviews { get; set; }
    }
}
