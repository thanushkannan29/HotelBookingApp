using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin audit log queries — scoped to the admin's own actions and hotel.</summary>
    [Route("api/admin/audit-logs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AdminAuditLogController(IAuditLogService auditLogService)
            => _auditLogService = auditLogService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged audit logs for the admin's hotel with optional keyword search.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] AuditLogQueryDto dto)
        {
            var result = await _auditLogService.GetAdminAuditLogsAsync(
                GetUserId(), dto.Page, dto.PageSize, dto.Search);
            return Ok(new { success = true, data = result });
        }
    }
}
