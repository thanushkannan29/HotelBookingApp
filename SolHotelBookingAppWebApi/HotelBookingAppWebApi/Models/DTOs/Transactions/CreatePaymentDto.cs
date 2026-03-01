using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Transactions
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }
    }
}
