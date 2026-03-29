using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    // ── WALLET ────────────────────────────────────────────────────────────────
    [Route("api/guest/wallet")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestWalletController : ControllerBase
    {
        private readonly IWalletService _service;
        public GuestWalletController(IWalletService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Get wallet balance and transaction history (paged)</summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetWalletAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Top up wallet balance</summary>
        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] TopUpWalletDto dto)
        {
            var result = await _service.TopUpAsync(GetUserId(), dto.Amount);
            return Ok(new { success = true, data = result });
        }
    }

    // ── PROMO CODES ───────────────────────────────────────────────────────────
    [Route("api/guest/promo-codes")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestPromoCodeController : ControllerBase
    {
        private readonly IPromoCodeService _service;
        public GuestPromoCodeController(IPromoCodeService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>List all promo codes for the current guest</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetGuestPromoCodesPagedAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Validate a promo code before booking</summary>
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidatePromoCodeDto dto)
        {
            var result = await _service.ValidateAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }
    }

    // ── QR PAYMENT ────────────────────────────────────────────────────────────
    [Route("api/guest/payment")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestPaymentController : ControllerBase
    {
        private readonly IReservationService _service;
        public GuestPaymentController(IReservationService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Get UPI QR code for a reservation payment</summary>
        [HttpGet("qr/{reservationId}")]
        public async Task<IActionResult> GetQr(Guid reservationId)
        {
            var result = await _service.GetPaymentQrAsync(GetUserId(), reservationId);
            return Ok(new { success = true, data = result });
        }
    }
}
