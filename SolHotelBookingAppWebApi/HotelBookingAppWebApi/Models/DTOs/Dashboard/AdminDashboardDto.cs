namespace HotelBookingAppWebApi.Models.DTOs.Dashboard
{
    /// <summary>Hotel stats, reservation counts, revenue, and review summary for the admin dashboard.</summary>
    public class AdminDashboardDto
    {
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? HotelImageUrl { get; set; }
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
}
