namespace HotelBookingAppWebApi.Models.DTOs.PromoCode
{
    public class PromoCodeValidationResultDto
    {
        public bool IsValid { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
