using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBookingAppWebApi.Controllers;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    public class HotelQueryDto : PageQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
    }
    // ── SUPERADMIN HOTEL MANAGEMENT ───────────────────────────────────────────
    [Route("api/superadmin/hotels")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminHotelController : ControllerBase
    {
        private readonly IHotelService _service;
        public SuperAdminHotelController(IHotelService service) => _service = service;

        /// <summary>
        /// Correction 9A: Paged list of all hotels with revenue and reservation stats.
        /// POST /api/superadmin/hotels/list
        /// Returns { totalCount, hotels } for Angular Material paginator.
        /// </summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetAll([FromBody] HotelQueryDto dto)
        {
            var result = await _service.GetAllHotelsForSuperAdminPagedAsync(dto.Page, dto.PageSize, dto.Search, dto.Status);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Block a hotel — prevents admin from activating it</summary>
        [HttpPatch("{id}/block")]
        public async Task<IActionResult> Block(Guid id)
        {
            await _service.BlockHotelAsync(id);
            return Ok(new { success = true, message = "Hotel has been blocked." });
        }

        /// <summary>Unblock a hotel — admin can now reactivate it</summary>
        [HttpPatch("{id}/unblock")]
        public async Task<IActionResult> Unblock(Guid id)
        {
            await _service.UnblockHotelAsync(id);
            return Ok(new { success = true, message = "Hotel has been unblocked." });
        }
    }

    // ── SUPERADMIN AUDIT LOGS ─────────────────────────────────────────────────
    [Route("api/superadmin/audit-logs")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminAuditLogController : ControllerBase
    {
        private readonly IAuditLogService _service;
        public SuperAdminAuditLogController(IAuditLogService service) => _service = service;

        /// <summary>View all audit logs with optional filters</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetAll([FromBody] AuditLogSuperAdminQueryDto dto)
        {
            Guid? hotelId = dto.HotelId != null ? Guid.Parse(dto.HotelId) : null;
            Guid? userId  = dto.UserId  != null ? Guid.Parse(dto.UserId)  : null;
            DateTime? dateFrom = dto.DateFrom != null ? DateTime.Parse(dto.DateFrom) : null;
            DateTime? dateTo   = dto.DateTo   != null ? DateTime.Parse(dto.DateTo)   : null;
            var result = await _service.GetAllAuditLogsAsync(dto.Page, dto.PageSize, hotelId, userId, dto.Action, dateFrom, dateTo);
            return Ok(new { success = true, data = result });
        }
    }

}
