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

        [HttpPatch("gst")]
        public async Task<IActionResult> UpdateGst([FromBody] UpdateHotelGstDto dto)
        {
            await _service.UpdateHotelGstAsync(GetUserId(), dto.GstPercent);
            return Ok(new { success = true, message = "GST updated successfully." });
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

        /// <summary>
        /// Correction 9A: Paged refund requests for this admin's hotel.
        /// GET /api/admin/refund-requests?page=1&amp;pageSize=10
        /// Returns { totalCount, refundRequests } for Angular Material paginator.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetHotelRefundRequestsPagedAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// Approve a refund request — triggers actual refund transaction.
        /// Body: { adminResponse, refundPaymentMethod, refundTransactionRef }
        /// Correction 8: RefundPaymentMethod and RefundTransactionRef are now saved.
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ProcessRefundDto dto)
        {
            var result = await _service.ApproveRefundAsync(id, GetUserId(), dto);
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

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _service.GetAdminAuditLogsAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }
    }
}
