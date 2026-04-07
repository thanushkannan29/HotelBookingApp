using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    /// <summary>Guest support — submit complaints and view own requests.</summary>
    [Route("api/guest/support")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestSupportController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;

        public GuestSupportController(ISupportRequestService supportRequestService)
            => _supportRequestService = supportRequestService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Submit a support complaint linked to the guest's account.</summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] GuestSupportRequestDto dto)
        {
            var result = await _supportRequestService.CreateGuestRequestAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result, message = "Your support request has been submitted." });
        }

        /// <summary>Returns paged support requests submitted by this guest.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] PageQueryDto dto)
        {
            var result = await _supportRequestService.GetGuestRequestsAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }
    }
}
