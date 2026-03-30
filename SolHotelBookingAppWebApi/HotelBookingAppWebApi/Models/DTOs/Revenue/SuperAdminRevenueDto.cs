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
        public DateTime CreatedAt { get; set; }
    }
}
