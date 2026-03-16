namespace HotelBookingAppWebApi.Models.DTOs.Dashboard
{
    public class AdminDashboardDto
    {
        public Guid HotelId { get; set; }

        public int TotalRooms { get; set; }

        public int TotalReservations { get; set; }

        public int ActiveReservations { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalReviews { get; set; }

        public decimal AverageRating { get; set; }
    }
}
