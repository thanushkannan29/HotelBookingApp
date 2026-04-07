using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin support — submit bug/issue reports and view own submissions.</summary>
    [Route("api/admin/support")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminSupportController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;

        public AdminSupportController(ISupportRequestService supportRequestService)
            => _supportRequestService = supportRequestService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Submit a bug or issue report to the platform team.</summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] AdminSupportRequestDto dto)
        {
            var result = await _supportRequestService.CreateAdminRequestAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result, message = "Your report has been submitted to the platform team." });
        }

        /// <summary>Returns paged support reports submitted by this admin.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] PageQueryDto dto)
        {
            var result = await _supportRequestService.GetAdminRequestsAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }
    }
}
