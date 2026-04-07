using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.PromoCode
{
    public class ValidatePromoCodeDto
    {
        [Required] public string Code { get; set; } = string.Empty;
        [Required] public Guid HotelId { get; set; }
        [Required] public decimal TotalAmount { get; set; }
    }
}
