using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/rooms")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomController : ControllerBase
    {
        private readonly IRoomService _service;

        public AdminRoomController(IRoomService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomDto dto)
        {
            try
            {
                await _service.AddRoomAsync(GetUserId(), dto);

                return Ok(new
                {
                    success = true,
                    message = "Room added successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomDto dto)
        {
            try
            {
                await _service.UpdateRoomAsync(GetUserId(), dto);

                return Ok(new
                {
                    success = true,
                    message = "Room updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{roomId}/status")]
        public async Task<IActionResult> Toggle(Guid roomId, [FromQuery] bool isActive)
        {
            try
            {
                await _service.ToggleRoomStatusAsync(GetUserId(), roomId, isActive);

                return Ok(new
                {
                    success = true,
                    message = "Room status updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var rooms = await _service.GetRoomsByHotelAsync(GetUserId(), pageNumber, pageSize);

                return Ok(new
                {
                    success = true,
                    data = rooms
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }


}


