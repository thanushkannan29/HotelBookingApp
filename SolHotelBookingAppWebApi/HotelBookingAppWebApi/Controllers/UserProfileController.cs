using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers
{
    /// <summary>User profile management — view, update, and booking history for all roles.</summary>
    [Route("api/user-profile")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserProfileController(IUserService userService)
            => _userService = userService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns the authenticated user's profile details.</summary>
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _userService.GetProfileAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Updates profile fields. Only non-null/non-empty values are applied.</summary>
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var result = await _userService.UpdateProfileAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns paged booking history for the authenticated guest.</summary>
        [HttpPost("booking-history")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetBookingHistory([FromBody] PaginationDto dto)
        {
            var result = await _userService.GetBookingHistoryAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }
    }
}
