using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/hotels")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminHotelController : ControllerBase
    {
        private readonly IHotelService _service;

        public AdminHotelController(IHotelService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateHotelDto dto)
        {
            await _service.UpdateHotelAsync(GetUserId(), dto);
            return Ok("Hotel updated successfully");
        }

        [HttpPatch("status")]
        public async Task<IActionResult> Toggle([FromQuery] bool isActive)
        {
            await _service.ToggleHotelStatusAsync(GetUserId(), isActive);
            return Ok("Hotel status updated");
        }
    }

}
