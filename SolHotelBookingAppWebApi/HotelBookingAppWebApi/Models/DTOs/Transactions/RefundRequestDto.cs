using System.ComponentModel.DataAnnotations;
namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class RefundRequestDto
    {
        [Required] public string Reason { get; set; } = string.Empty;
    }
}
