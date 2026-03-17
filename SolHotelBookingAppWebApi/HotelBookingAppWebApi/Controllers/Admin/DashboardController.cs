using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var result = await _service.GetAdminDashboardAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("guest")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GuestDashboard()
        {
            var result = await _service.GetGuestDashboardAsync(GetUserId());
            return Ok(result);
        }

        [HttpGet("superadmin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SuperAdminDashboard()
        {
            var result = await _service.GetSuperAdminDashboardAsync();
            return Ok(result);
        }
    }

}
