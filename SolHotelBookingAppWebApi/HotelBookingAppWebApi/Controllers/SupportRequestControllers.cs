using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HotelBookingAppWebApi.Controllers;

namespace HotelBookingAppWebApi.Controllers
{
    public class SupportQueryDto : PageQueryDto
    {
        public string? Status { get; set; }
        public string? Role { get; set; }
        public string? Search { get; set; }
    }
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

        /// <summary>POST /api/guest/support/list — guest views own requests</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetMine([FromBody] PageQueryDto dto)
        {
            var result = await _service.GetGuestRequestsAsync(GetUserId(), dto.Page, dto.PageSize);
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

        /// <summary>POST /api/admin/support/list — admin views own reports</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetMine([FromBody] PageQueryDto dto)
        {
            var result = await _service.GetAdminRequestsAsync(GetUserId(), dto.Page, dto.PageSize);
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

        /// <summary>POST /api/superadmin/support/list — all requests with filters</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetAll([FromBody] SupportQueryDto dto)
        {
            var result = await _service.GetAllRequestsAsync(dto.Status, dto.Role, dto.Search, dto.Page, dto.PageSize);
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
