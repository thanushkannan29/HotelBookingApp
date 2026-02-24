using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/hotel")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminHotelController : ControllerBase
    {
        private readonly IHotelService _service;

        public AdminHotelController(IHotelService service)
        {
            _service = service;
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(UpdateHotelDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _service.UpdateHotelAsync(userId, dto);
            return Ok("Hotel updated successfully");
        }

        [HttpPut("toggle-status")]
        public async Task<IActionResult> Toggle(bool isActive)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _service.ToggleHotelStatusAsync(userId, isActive);
            return Ok("Hotel status updated");
        }
    }
}
