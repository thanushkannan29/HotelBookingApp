using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin wallet view — read-only access to a guest's wallet balance.</summary>
    [Route("api/admin/wallet")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminWalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public AdminWalletController(IWalletService walletService)
            => _walletService = walletService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns the wallet balance for a specific guest.</summary>
        [HttpGet("guest/{guestUserId}")]
        public async Task<IActionResult> GetGuestWallet(Guid guestUserId)
        {
            var result = await _walletService.GetGuestWalletByAdminAsync(GetUserId(), guestUserId);
            return Ok(new { success = true, data = result });
        }
    }
}
