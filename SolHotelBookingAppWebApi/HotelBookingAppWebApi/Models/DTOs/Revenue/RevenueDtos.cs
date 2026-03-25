namespace HotelBookingAppWebApi.Models.DTOs.Revenue
{
    public class SuperAdminRevenueDto
    {
        public Guid SuperAdminRevenueId { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public decimal ReservationAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public string SuperAdminUpiId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PagedRevenueResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<SuperAdminRevenueDto> Items { get; set; } = new List<SuperAdminRevenueDto>();
    }

    public class RevenueSummaryDto
    {
        public decimal TotalCommissionEarned { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalSent { get; set; }
        public int PendingCount { get; set; }
        public int SentCount { get; set; }
    }
}
