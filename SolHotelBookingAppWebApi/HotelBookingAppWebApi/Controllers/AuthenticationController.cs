using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers
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

        /// <summary>Register a new guest account</summary>
        [HttpPost("register-guest")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterGuest([FromBody] RegisterUserDto dto)
        {
            var result = await _authService.RegisterGuestAsync(dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Register a new hotel admin + hotel in one step</summary>
        [HttpPost("register-hotel-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterHotelAdmin([FromBody] RegisterHotelAdminDto dto)
        {
            var result = await _authService.RegisterHotelAdminAsync(dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Login for all roles</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(new { success = true, data = result });
        }
    }
}
