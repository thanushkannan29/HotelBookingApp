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
            var result = await _authService.RegisterGuestAsync(dto);
            return Ok(result);
        }

        [HttpPost("register-hotel-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterHotelAdmin([FromBody] RegisterHotelAdminDto dto)
        {
            var result = await _authService.RegisterHotelAdminAsync(dto);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
    }

}
