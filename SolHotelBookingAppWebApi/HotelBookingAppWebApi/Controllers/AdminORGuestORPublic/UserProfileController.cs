using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserService _service;

        public UserProfileController(IUserService service)
        {
            _service = service;
        }

        private Guid GetUserId()
        {
            return Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // ============================================
        // GET PROFILE (Guest/Admin)
        // ============================================
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _service.GetProfileAsync(GetUserId());
            return Ok(result);
        }

        // ============================================
        // UPDATE PROFILE
        // ============================================
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(UpdateUserProfileDto dto)
        {
            var result = await _service.UpdateProfileAsync(GetUserId(), dto);
            return Ok(result);
        }

        // ============================================
        // BOOKING HISTORY (Guest)
        // ============================================
        [HttpGet("booking-history")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetBookingHistory(
            int page = 1,
            int pageSize = 10)
        {
            var result = await _service
                .GetBookingHistoryAsync(GetUserId(), page, pageSize);

            return Ok(result);
        }
    }
}
