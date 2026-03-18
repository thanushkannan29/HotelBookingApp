using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthenticationController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register-guest")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterGuest([FromBody] RegisterUserDto dto)
        {
            try
            {
                var result = await _authService.RegisterGuestAsync(dto);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("register-hotel-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterHotelAdmin([FromBody] RegisterHotelAdminDto dto)
        {
            try
            {
                var result = await _authService.RegisterHotelAdminAsync(dto);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }


}
