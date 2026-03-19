using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    // ── SUPERADMIN HOTEL MANAGEMENT ───────────────────────────────────────────
    [Route("api/superadmin/hotels")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminHotelController : ControllerBase
    {
        private readonly IHotelService _service;
        public SuperAdminHotelController(IHotelService service) => _service = service;

        /// <summary>List all hotels with revenue and reservation stats</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllHotelsForSuperAdminAsync();
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

        /// <summary>View all audit logs across the entire system</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _service.GetAllAuditLogsAsync(page, pageSize);
            return Ok(new { success = true, data = result });
        }
    }
}
