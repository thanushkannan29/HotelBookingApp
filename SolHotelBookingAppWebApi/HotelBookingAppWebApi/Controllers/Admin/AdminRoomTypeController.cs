using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/roomtypes")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomTypeController : ControllerBase
    {
        private readonly IRoomTypeService _service;

        public AdminRoomTypeController(IRoomTypeService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomTypeDto dto)
        {
            await _service.AddRoomTypeAsync(GetUserId(), dto);
            return Ok("RoomType added successfully");
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomTypeDto dto)
        {
            await _service.UpdateRoomTypeAsync(GetUserId(), dto);
            return Ok("RoomType updated");
        }

        [HttpPatch("{roomTypeId}/status")]
        public async Task<IActionResult> ToggleStatus(Guid roomTypeId, [FromQuery] bool isActive)
        {
            await _service.ToggleRoomTypeStatusAsync(GetUserId(), roomTypeId, isActive);
            return Ok("RoomType status updated");
        }

        [HttpPost("rate")]
        public async Task<IActionResult> AddRate([FromBody] CreateRoomTypeRateDto dto)
        {
            await _service.AddRateAsync(GetUserId(), dto);
            return Ok("Rate added");
        }

        [HttpPut("rate")]
        public async Task<IActionResult> UpdateRate([FromBody] UpdateRoomTypeRateDto dto)
        {
            await _service.UpdateRateAsync(GetUserId(), dto);
            return Ok("Rate updated");
        }

        [HttpPost("rate-by-date")]
        public async Task<IActionResult> GetRate([FromBody] GetRateByDateRequestDto dto)
        {
            var rate = await _service.GetRateByDateAsync(GetUserId(), dto);
            return Ok(rate);
        }
    }

}
