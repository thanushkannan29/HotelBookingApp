using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.PromoCode
{
    public class PromoCodeResponseDto
    {
        public Guid PromoCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public Guid HotelId { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public string Status { get; set; } = string.Empty; // Active / Used / Expired
    }

    public class ValidatePromoCodeDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }
    }

    public class PromoCodeValidationResultDto
    {
        public bool IsValid { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
