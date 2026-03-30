using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers
{
    // ── PUBLIC (unauthenticated) ──────────────────────────────────────────────
    [Route("api/support")]
    [ApiController]
    [AllowAnonymous]
    public class PublicSupportController : ControllerBase
    {
        private readonly ISupportRequestService _service;
        public PublicSupportController(ISupportRequestService service) => _service = service;

        /// <summary>POST /api/support — anyone can submit a contact/support request</summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] PublicSupportRequestDto dto)
        {
            var result = await _service.CreatePublicRequestAsync(dto);
            return Ok(new { success = true, data = result, message = "Your request has been submitted. We'll get back to you soon." });
        }
    }

    // ── GUEST ─────────────────────────────────────────────────────────────────
    [Route("api/guest/support")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestSupportController : ControllerBase
    {
        private readonly ISupportRequestService _service;
        public GuestSupportController(ISupportRequestService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>POST /api/guest/support — guest submits a complaint</summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] GuestSupportRequestDto dto)
        {
            var result = await _service.CreateGuestRequestAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result, message = "Your support request has been submitted." });
        }

        /// <summary>GET /api/guest/support — guest views own requests</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetGuestRequestsAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }
    }

    // ── ADMIN ─────────────────────────────────────────────────────────────────
    [Route("api/admin/support")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminSupportController : ControllerBase
    {
        private readonly ISupportRequestService _service;
        public AdminSupportController(ISupportRequestService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>POST /api/admin/support — admin submits a bug/issue report</summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] AdminSupportRequestDto dto)
        {
            var result = await _service.CreateAdminRequestAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result, message = "Your report has been submitted to the platform team." });
        }

        /// <summary>GET /api/admin/support — admin views own reports</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAdminRequestsAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }
    }

    // ── SUPERADMIN ────────────────────────────────────────────────────────────
    [Route("api/superadmin/support")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminSupportController : ControllerBase
    {
        private readonly ISupportRequestService _service;
        public SuperAdminSupportController(ISupportRequestService service) => _service = service;

        /// <summary>GET /api/superadmin/support — all requests with filters</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] string? role = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllRequestsAsync(status, role, search, page, pageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>PATCH /api/superadmin/support/{id}/respond — respond to a request</summary>
        [HttpPatch("{id}/respond")]
        public async Task<IActionResult> Respond(Guid id, [FromBody] RespondSupportRequestDto dto)
        {
            var result = await _service.RespondAsync(id, dto);
            return Ok(new { success = true, data = result, message = "Response sent successfully." });
        }
    }
}
