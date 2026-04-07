using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    /// <summary>SuperAdmin revenue — view platform commission records and totals.</summary>
    [Route("api/superadmin/revenue")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminRevenueController : ControllerBase
    {
        private readonly ISuperAdminRevenueService _revenueService;

        public SuperAdminRevenueController(ISuperAdminRevenueService revenueService)
            => _revenueService = revenueService;

        /// <summary>Returns paged commission records ordered by most recent.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] RevenueQueryDto dto)
        {
            var result = await _revenueService.GetAllRevenueAsync(dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns the total commission earned to date.</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _revenueService.GetSummaryAsync();
            return Ok(new { success = true, data = result });
        }
    }
}
