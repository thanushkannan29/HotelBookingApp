using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    /// <summary>Guest wallet — view balance, transaction history, and top up.</summary>
    [Route("api/guest/wallet")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestWalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public GuestWalletController(IWalletService walletService)
            => _walletService = walletService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged wallet transaction history and current balance.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetWallet([FromBody] PageQueryDto dto)
        {
            var result = await _walletService.GetWalletAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Add funds to the wallet.</summary>
        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] TopUpWalletDto dto)
        {
            var result = await _walletService.TopUpAsync(GetUserId(), dto.Amount);
            return Ok(new { success = true, data = result });
        }
    }
}
