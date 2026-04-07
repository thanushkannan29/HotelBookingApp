using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    /// <summary>Guest payment — UPI QR code generation for reservation payments.</summary>
    [Route("api/guest/payment")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestPaymentController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public GuestPaymentController(IReservationService reservationService)
            => _reservationService = reservationService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns a UPI QR code for the guest to pay for a pending reservation.</summary>
        [HttpGet("qr/{reservationId}")]
        public async Task<IActionResult> GetQrCode(Guid reservationId)
        {
            var result = await _reservationService.GetPaymentQrAsync(GetUserId(), reservationId);
            return Ok(new { success = true, data = result });
        }
    }
}
