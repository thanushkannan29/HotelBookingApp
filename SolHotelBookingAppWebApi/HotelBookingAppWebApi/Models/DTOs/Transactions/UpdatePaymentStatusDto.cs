using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class UpdatePaymentStatusDto
    {
        [Required]
        public PaymentStatus Status { get; set; }
    }
}
