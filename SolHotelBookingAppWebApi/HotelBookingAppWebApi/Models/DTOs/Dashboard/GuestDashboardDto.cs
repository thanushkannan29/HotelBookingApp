namespace HotelBookingAppWebApi.Models.DTOs.Dashboard
{
    public class GuestDashboardDto
    {
        public int TotalBookings { get; set; }

        public int ActiveBookings { get; set; }

        public int CompletedBookings { get; set; }

        public int CancelledBookings { get; set; }

        public decimal TotalSpent { get; set; }
    }
}
