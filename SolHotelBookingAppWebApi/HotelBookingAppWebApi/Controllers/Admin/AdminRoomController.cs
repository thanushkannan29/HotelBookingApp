using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin room management — add, update, toggle status, list, and occupancy view.</summary>
    [Route("api/admin/rooms")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IReservationService _reservationService;

        public AdminRoomController(IRoomService roomService, IReservationService reservationService)
        {
            _roomService = roomService;
            _reservationService = reservationService;
        }

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Add a new physical room to the hotel.</summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomDto dto)
        {
            await _roomService.AddRoomAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room added successfully." });
        }

        /// <summary>Update room number, floor, or room type.</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomDto dto)
        {
            await _roomService.UpdateRoomAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room updated successfully." });
        }

        /// <summary>Activate or deactivate a room.</summary>
        [HttpPatch("{roomId}/status")]
        public async Task<IActionResult> ToggleStatus(Guid roomId, [FromQuery] bool isActive)
        {
            await _roomService.ToggleRoomStatusAsync(GetUserId(), roomId, isActive);
            return Ok(new { success = true, message = "Room status updated." });
        }

        /// <summary>Returns paged list of all rooms in the hotel with total count.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] PageQueryDto dto)
        {
            var userId = GetUserId();
            var rooms = await _roomService.GetRoomsByHotelAsync(userId, dto.PageNumber, dto.PageSize);
            var totalCount = await _roomService.GetRoomCountByHotelAsync(userId);
            return Ok(new { success = true, data = new { totalCount, items = rooms } });
        }

        /// <summary>Returns all rooms with their occupancy status for a given date.</summary>
        [HttpGet("occupancy")]
        public async Task<IActionResult> GetOccupancy([FromQuery] DateOnly date)
        {
            var result = await _reservationService.GetRoomOccupancyAsync(GetUserId(), date);
            return Ok(new { success = true, data = result });
        }
    }
}
