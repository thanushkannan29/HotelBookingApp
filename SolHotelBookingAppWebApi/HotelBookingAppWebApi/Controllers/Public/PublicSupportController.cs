using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.Public
{
    /// <summary>Public support — unauthenticated contact/support form submission.</summary>
    [Route("api/support")]
    [ApiController]
    public class PublicSupportController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;

        public PublicSupportController(ISupportRequestService supportRequestService)
            => _supportRequestService = supportRequestService;

        /// <summary>Anyone can submit a contact or support request.</summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] PublicSupportRequestDto dto)
        {
            var result = await _supportRequestService.CreatePublicRequestAsync(dto);
            return Ok(new { success = true, data = result, message = "Your request has been submitted. We'll get back to you soon." });
        }
    }
}
