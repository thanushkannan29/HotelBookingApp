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
            public async Task<IActionResult> Add(CreateRoomDto dto)
            {
                await _service.AddRoomAsync(GetUserId(), dto);
                return Ok("Room added successfully");
            }

            [HttpPut]
            public async Task<IActionResult> Update(UpdateRoomDto dto)
            {
                await _service.UpdateRoomAsync(GetUserId(), dto);
                return Ok("Room updated");
            }

            [HttpPatch("{roomId}/status")]
            public async Task<IActionResult> Toggle(Guid roomId, bool isActive)
            {
                await _service.ToggleRoomStatusAsync(GetUserId(), roomId, isActive);
                return Ok("Room status updated");
            }

            [HttpGet]
            public async Task<IActionResult> List(int pageNumber = 1, int pageSize = 10)
            {
                var rooms = await _service.GetRoomsByHotelAsync(GetUserId(), pageNumber, pageSize);
                return Ok(rooms);
            }
        }
    }


