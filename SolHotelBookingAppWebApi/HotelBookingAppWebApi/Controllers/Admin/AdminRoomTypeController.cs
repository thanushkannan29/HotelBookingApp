using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/roomtype")]
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
        public async Task<IActionResult> Add(CreateRoomTypeDto dto)
        {
            await _service.AddRoomTypeAsync(GetUserId(), dto);
            return Ok("RoomType added successfully");
        }

        [HttpPut]
        public async Task<IActionResult> Update(UpdateRoomTypeDto dto)
        {
            await _service.UpdateRoomTypeAsync(GetUserId(), dto);
            return Ok("RoomType updated");
        }

        [HttpPut("{roomTypeId}/toggle-status")]

        public async Task<IActionResult> ToggleStatus(Guid roomTypeId, bool isActive)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            await _service.ToggleRoomTypeStatusAsync(userId,roomTypeId,isActive);

            return Ok("RoomType status updated");
        }


        [HttpPost("rate")]
        public async Task<IActionResult> AddRate(CreateRoomTypeRateDto dto)
        {
            await _service.AddRateAsync(GetUserId(), dto);
            return Ok("Rate added");
        }

        [HttpPut("rate")]
        public async Task<IActionResult> UpdateRate(UpdateRoomTypeRateDto dto)
        {
            await _service.UpdateRateAsync(GetUserId(), dto);
            return Ok("Rate updated");
        }
       


        [HttpPost("rate-by-date")]
        public async Task<IActionResult> GetRate(GetRateByDateRequestDto dto)
        {
            var rate = await _service.GetRateByDateAsync(GetUserId(), dto);
            return Ok(rate);
        }
    }
}
