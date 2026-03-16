namespace HotelBookingAppWebApi.Models.DTOs.Dashboard
{
    public class SuperAdminDashboardDto
    {
        public int TotalHotels { get; set; }

        public int TotalUsers { get; set; }

        public int TotalReservations { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalReviews { get; set; }
    }
}
