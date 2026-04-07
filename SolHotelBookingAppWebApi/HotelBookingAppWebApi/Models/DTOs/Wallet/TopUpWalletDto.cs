using System.ComponentModel.DataAnnotations;

namespace HotelBookingAppWebApi.Models.DTOs.Wallet
{
    /// <summary>Payload for adding funds to the wallet.</summary>
    public class TopUpWalletDto
    {
        [Required, Range(1, 100000)]
        public decimal Amount { get; set; }
    }
}
