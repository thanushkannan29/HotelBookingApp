using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.RefundRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    // ── HOTEL ─────────────────────────────────────────────────────────────────
    [Route("api/admin/hotels")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminHotelController : ControllerBase
    {
        private readonly IHotelService _service;
        public AdminHotelController(IHotelService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateHotelDto dto)
        {
            await _service.UpdateHotelAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Hotel updated successfully." });
        }

        [HttpPatch("status")]
        public async Task<IActionResult> Toggle([FromQuery] bool isActive)
        {
            await _service.ToggleHotelStatusAsync(GetUserId(), isActive);
            return Ok(new { success = true, message = "Hotel status updated successfully." });
        }
    }

    // ── REFUND REQUESTS ───────────────────────────────────────────────────────
    [Route("api/admin/refund-requests")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRefundRequestController : ControllerBase
    {
        private readonly IRefundRequestService _service;
        public AdminRefundRequestController(IRefundRequestService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>List all refund requests for this admin's hotel</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetHotelRefundRequestsAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Approve a refund request → triggers actual refund transaction</summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ProcessRefundDto dto)
        {
            var result = await _service.ApproveRefundAsync(id, GetUserId(), dto.AdminResponse);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Reject a refund request</summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] ProcessRefundDto dto)
        {
            var result = await _service.RejectRefundAsync(id, GetUserId(), dto.AdminResponse);
            return Ok(new { success = true, data = result });
        }
    }

    // ── AUDIT LOGS ────────────────────────────────────────────────────────────
    [Route("api/admin/audit-logs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAuditLogController : ControllerBase
    {
        private readonly IAuditLogService _service;
        public AdminAuditLogController(IAuditLogService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _service.GetAdminAuditLogsAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }
    }
}
