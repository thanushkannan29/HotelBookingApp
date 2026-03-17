using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/user-profile")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserService _service;

        public UserProfileController(IUserService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            return Ok(await _service.GetProfileAsync(GetUserId()));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            return Ok(await _service.UpdateProfileAsync(GetUserId(), dto));
        }

        //  Can convert to POST (mam suggestion valid here)
        [HttpPost("booking-history")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetBookingHistory([FromBody] PaginationDto dto)
        {
            var result = await _service.GetBookingHistoryAsync(
                GetUserId(),
                dto.Page,
                dto.PageSize);

            return Ok(result);
        }

    }

}
