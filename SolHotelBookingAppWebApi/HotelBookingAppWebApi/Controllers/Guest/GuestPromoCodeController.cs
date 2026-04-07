using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    /// <summary>Guest promo code management — list and validate promo codes.</summary>
    [Route("api/guest/promo-codes")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestPromoCodeController : ControllerBase
    {
        private readonly IPromoCodeService _promoCodeService;

        public GuestPromoCodeController(IPromoCodeService promoCodeService)
            => _promoCodeService = promoCodeService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged promo codes for the guest with optional status filter.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] PromoQueryDto dto)
        {
            var result = await _promoCodeService.GetGuestPromoCodesPagedAsync(
                GetUserId(), dto.Page, dto.PageSize, dto.Status);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Validates a promo code for a specific hotel and booking amount.</summary>
        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidatePromoCodeDto dto)
        {
            var result = await _promoCodeService.ValidateAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }
    }
}
