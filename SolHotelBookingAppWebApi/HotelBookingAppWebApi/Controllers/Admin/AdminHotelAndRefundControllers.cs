using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    public class AuditLogQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
    }
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

        [HttpPatch("gst")]
        public async Task<IActionResult> UpdateGst([FromBody] UpdateHotelGstDto dto)
        {
            await _service.UpdateHotelGstAsync(GetUserId(), dto.GstPercent);
            return Ok(new { success = true, message = "GST updated successfully." });
        }
    }

    // ── ADMIN TRANSACTIONS (mark-failed) ──────────────────────────────────────
    [Route("api/admin/transactions")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminTransactionController : ControllerBase
    {
        private readonly ITransactionService _service;
        public AdminTransactionController(ITransactionService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Correction 7E: Admin marks a transaction as Failed.
        /// Sets Transaction.Status = Failed and resets Reservation.Status = Pending
        /// so the guest can attempt payment again.
        /// PATCH /api/admin/transactions/{transactionId}/mark-failed
        /// </summary>
        [HttpPatch("{transactionId}/mark-failed")]
        public async Task<IActionResult> MarkFailed(Guid transactionId)
        {
            await _service.MarkTransactionFailedAsync(transactionId, GetUserId());
            return Ok(new { success = true, message = "Transaction marked as failed. Reservation reset to Pending." });
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

        [HttpPost("list")]
        public async Task<IActionResult> GetAuditLogs([FromBody] AuditLogQueryDto dto)
        {
            var result = await _service.GetAdminAuditLogsAsync(GetUserId(), dto.Page, dto.PageSize, dto.Search);
            return Ok(new { success = true, data = result });
        }
    }
}
