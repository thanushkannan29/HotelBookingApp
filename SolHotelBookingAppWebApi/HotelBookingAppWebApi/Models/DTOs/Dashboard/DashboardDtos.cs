namespace HotelBookingAppWebApi.Models.DTOs.Dashboard
{
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

    public class AdminDashboardDto
    {
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsBlockedBySuperAdmin { get; set; }
        public int TotalRooms { get; set; }
        public int ActiveRooms { get; set; }
        public int TotalRoomTypes { get; set; }
        public int TotalReservations { get; set; }
        public int PendingReservations { get; set; }
        public int ActiveReservations { get; set; }
        public int CompletedReservations { get; set; }
        public int CancelledReservations { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalReviews { get; set; }
        public decimal AverageRating { get; set; }
    }

    public class GuestDashboardDto
    {
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
