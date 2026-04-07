using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    /// <summary>SuperAdmin audit log queries — all logs with optional filters.</summary>
    [Route("api/superadmin/audit-logs")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminAuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public SuperAdminAuditLogController(IAuditLogService auditLogService)
            => _auditLogService = auditLogService;

        /// <summary>Returns paged audit logs with optional hotel, user, action, and date filters.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] AuditLogSuperAdminQueryDto dto)
        {
            Guid? hotelId   = dto.HotelId   is not null ? Guid.Parse(dto.HotelId)   : null;
            Guid? userId    = dto.UserId    is not null ? Guid.Parse(dto.UserId)    : null;
            DateTime? from  = dto.DateFrom  is not null ? DateTime.Parse(dto.DateFrom) : null;
            DateTime? to    = dto.DateTo    is not null ? DateTime.Parse(dto.DateTo)   : null;

            var result = await _auditLogService.GetAllAuditLogsAsync(
                dto.Page, dto.PageSize, hotelId, userId, dto.Action, from, to);
            return Ok(new { success = true, data = result });
        }
    }
}
