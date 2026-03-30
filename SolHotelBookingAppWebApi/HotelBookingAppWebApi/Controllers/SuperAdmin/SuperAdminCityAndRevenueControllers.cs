using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HotelBookingAppWebApi.Controllers;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    public class AmenityRequestQueryDto : PageQueryDto
    {
        public string? Status { get; set; } = "All";
    }

    public class RevenueQueryDto : PageQueryDto { }
    // ── AMENITY REQUESTS ──────────────────────────────────────────────────────
    [Route("api/superadmin/amenity-requests")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminAmenityRequestController : ControllerBase
    {
        private readonly IAmenityRequestService _service;
        public SuperAdminAmenityRequestController(IAmenityRequestService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("list")]
        public async Task<IActionResult> GetAll([FromBody] AmenityRequestQueryDto dto)
        {
            var result = await _service.GetAllRequestsAsync(dto.Status ?? "All", dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var result = await _service.ApproveRequestAsync(id, GetUserId());
            return Ok(new { success = true, data = result });
        }

        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAmenityRequestDto dto)
        {
            var result = await _service.RejectRequestAsync(id, GetUserId(), dto.Note);
            return Ok(new { success = true, data = result });
        }
    }

    // ── REVENUE ───────────────────────────────────────────────────────────────
    [Route("api/superadmin/revenue")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminRevenueController : ControllerBase
    {
        private readonly ISuperAdminRevenueService _service;
        public SuperAdminRevenueController(ISuperAdminRevenueService service) => _service = service;

        [HttpPost("list")]
        public async Task<IActionResult> GetAll([FromBody] RevenueQueryDto dto)
        {
            var result = await _service.GetAllRevenueAsync(dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _service.GetSummaryAsync();
            return Ok(new { success = true, data = result });
        }
    }
}
