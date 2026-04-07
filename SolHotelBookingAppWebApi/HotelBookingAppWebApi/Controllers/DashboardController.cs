using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers
{
    /// <summary>Role-specific dashboard statistics — Admin, Guest, and SuperAdmin.</summary>
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
            => _dashboardService = dashboardService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns hotel stats, reservation counts, revenue, and review summary.</summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var result = await _dashboardService.GetAdminDashboardAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns booking counts and total spend for the guest.</summary>
        [HttpGet("guest")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GuestDashboard()
        {
            var result = await _dashboardService.GetGuestDashboardAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns platform-wide totals — hotels, users, reservations, revenue, and reviews.</summary>
        [HttpGet("superadmin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SuperAdminDashboard()
        {
            var result = await _dashboardService.GetSuperAdminDashboardAsync();
            return Ok(new { success = true, data = result });
        }
    }
}
