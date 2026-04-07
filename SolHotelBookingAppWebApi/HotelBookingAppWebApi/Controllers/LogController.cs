using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers
{
    /// <summary>Application log queries — user's own logs and SuperAdmin full access.</summary>
    [Route("api/logs")]
    [ApiController]
    [Authorize]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;

        public LogController(ILogService logService)
            => _logService = logService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged logs for the authenticated user.</summary>
        [HttpPost("my-logs")]
        public async Task<IActionResult> GetMyLogs([FromBody] PageQueryDto dto)
        {
            var result = await _logService.GetUserLogsAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>SuperAdmin: returns all logs paged with optional keyword search.</summary>
        [HttpPost("list")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAll([FromBody] LogQueryDto dto)
        {
            var result = await _logService.GetAllLogsAsync(dto.Page, dto.PageSize, dto.Search);
            return Ok(new { success = true, data = result });
        }
    }
}
