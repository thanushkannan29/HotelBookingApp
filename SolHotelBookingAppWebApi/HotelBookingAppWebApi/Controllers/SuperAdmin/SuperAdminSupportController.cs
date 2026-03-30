using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    /// <summary>SuperAdmin support management — view all requests and respond.</summary>
    [Route("api/superadmin/support")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminSupportController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;

        public SuperAdminSupportController(ISupportRequestService supportRequestService)
            => _supportRequestService = supportRequestService;

        /// <summary>Returns paged support requests with optional status, role, and search filters.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] SupportQueryDto dto)
        {
            var result = await _supportRequestService.GetAllRequestsAsync(
                dto.Status, dto.Role, dto.Search, dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Respond to a support request and update its status.</summary>
        [HttpPatch("{id}/respond")]
        public async Task<IActionResult> Respond(Guid id, [FromBody] RespondSupportRequestDto dto)
        {
            var result = await _supportRequestService.RespondAsync(id, dto);
            return Ok(new { success = true, data = result, message = "Response sent successfully." });
        }
    }
}
