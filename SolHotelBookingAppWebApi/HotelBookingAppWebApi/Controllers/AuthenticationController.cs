using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthenticationController(IAuthService authService)
        {
            _authService = authService;
        }

        // REGISTER GUEST

        [HttpPost("register-guest")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterGuest(RegisterUserDto dto)
        {
            try
            {
                var result = await _authService.RegisterGuestAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // REGISTER HOTEL ADMIN

        [HttpPost("register-hotel-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterHotelAdmin(RegisterHotelAdminDto dto)
        {
            try
            {
                var result = await _authService.RegisterHotelAdminAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // LOGIN (Guest or Admin)

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        
    }
}
