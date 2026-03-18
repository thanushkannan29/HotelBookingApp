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
            try
            {
                await _service.AddRoomTypeAsync(GetUserId(), dto);

                return Ok(new { success = true, message = "RoomType added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomTypeDto dto)
        {
            try
            {
                await _service.UpdateRoomTypeAsync(GetUserId(), dto);

                return Ok(new { success = true, message = "RoomType updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{roomTypeId}/status")]
        public async Task<IActionResult> ToggleStatus(Guid roomTypeId, [FromQuery] bool isActive)
        {
            try
            {
                await _service.ToggleRoomTypeStatusAsync(GetUserId(), roomTypeId, isActive);

                return Ok(new { success = true, message = "RoomType status updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("rate")]
        public async Task<IActionResult> AddRate([FromBody] CreateRoomTypeRateDto dto)
        {
            try
            {
                await _service.AddRateAsync(GetUserId(), dto);

                return Ok(new { success = true, message = "Rate added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("rate")]
        public async Task<IActionResult> UpdateRate([FromBody] UpdateRoomTypeRateDto dto)
        {
            try
            {
                await _service.UpdateRateAsync(GetUserId(), dto);

                return Ok(new { success = true, message = "Rate updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("rate-by-date")]
        public async Task<IActionResult> GetRate([FromBody] GetRateByDateRequestDto dto)
        {
            try
            {
                var rate = await _service.GetRateByDateAsync(GetUserId(), dto);

                return Ok(new { success = true, data = rate });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }


}
