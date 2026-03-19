using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    // ── ROOMS ─────────────────────────────────────────────────────────────────
    [Route("api/admin/rooms")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomController : ControllerBase
    {
        private readonly IRoomService _service;
        public AdminRoomController(IRoomService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomDto dto)
        {
            await _service.AddRoomAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room added successfully." });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomDto dto)
        {
            await _service.UpdateRoomAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room updated successfully." });
        }

        [HttpPatch("{roomId}/status")]
        public async Task<IActionResult> Toggle(Guid roomId, [FromQuery] bool isActive)
        {
            await _service.ToggleRoomStatusAsync(GetUserId(), roomId, isActive);
            return Ok(new { success = true, message = "Room status updated." });
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var rooms = await _service.GetRoomsByHotelAsync(GetUserId(), pageNumber, pageSize);
            return Ok(new { success = true, data = rooms });
        }
    }

    // ── ROOM TYPES ────────────────────────────────────────────────────────────
    [Route("api/admin/roomtypes")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomTypeController : ControllerBase
    {
        private readonly IRoomTypeService _service;
        public AdminRoomTypeController(IRoomTypeService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var result = await _service.GetRoomTypesByHotelAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomTypeDto dto)
        {
            await _service.AddRoomTypeAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "RoomType added successfully." });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomTypeDto dto)
        {
            await _service.UpdateRoomTypeAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "RoomType updated successfully." });
        }

        [HttpPatch("{roomTypeId}/status")]
        public async Task<IActionResult> ToggleStatus(Guid roomTypeId, [FromQuery] bool isActive)
        {
            await _service.ToggleRoomTypeStatusAsync(GetUserId(), roomTypeId, isActive);
            return Ok(new { success = true, message = "RoomType status updated." });
        }

        [HttpPost("rate")]
        public async Task<IActionResult> AddRate([FromBody] CreateRoomTypeRateDto dto)
        {
            await _service.AddRateAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Rate added successfully." });
        }

        [HttpPut("rate")]
        public async Task<IActionResult> UpdateRate([FromBody] UpdateRoomTypeRateDto dto)
        {
            await _service.UpdateRateAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Rate updated successfully." });
        }

        [HttpPost("rate-by-date")]
        public async Task<IActionResult> GetRate([FromBody] GetRateByDateRequestDto dto)
        {
            var rate = await _service.GetRateByDateAsync(GetUserId(), dto);
            return Ok(new { success = true, data = rate });
        }
    }

    // ── INVENTORY ─────────────────────────────────────────────────────────────
    [Route("api/admin/inventory")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : ControllerBase
    {
        private readonly IInventoryService _service;
        public AdminInventoryController(IInventoryService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateInventoryDto dto)
        {
            await _service.AddInventoryAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Inventory added successfully." });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateInventoryDto dto)
        {
            await _service.UpdateInventoryAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Inventory updated successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] Guid roomTypeId,
            [FromQuery] DateOnly start,
            [FromQuery] DateOnly end)
        {
            var data = await _service.GetInventoryAsync(GetUserId(), roomTypeId, start, end);
            return Ok(new { success = true, data });
        }
    }

    // ── RESERVATIONS ──────────────────────────────────────────────────────────
    [Route("api/admin/reservations")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminReservationController : ControllerBase
    {
        private readonly IReservationService _service;
        public AdminReservationController(IReservationService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>List all reservations for this admin's hotel (paged)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetHotelReservationsAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Mark a confirmed reservation as completed</summary>
        [HttpPatch("{code}/complete")]
        public async Task<IActionResult> Complete(string code)
        {
            await _service.CompleteReservationAsync(code);
            return Ok(new { success = true, message = "Reservation marked as completed." });
        }
    }
}
