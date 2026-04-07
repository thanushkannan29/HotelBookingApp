using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin room type management — CRUD, status toggle, and pricing rates.</summary>
    [Route("api/admin/roomtypes")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoomTypeController : ControllerBase
    {
        private readonly IRoomTypeService _roomTypeService;

        public AdminRoomTypeController(IRoomTypeService roomTypeService)
            => _roomTypeService = roomTypeService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged room types for the hotel.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] PageQueryDto dto)
        {
            var result = await _roomTypeService.GetRoomTypesByHotelPagedAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Create a new room type with optional amenity associations.</summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateRoomTypeDto dto)
        {
            await _roomTypeService.AddRoomTypeAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room type added successfully." });
        }

        /// <summary>Update room type details and replace amenity associations.</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoomTypeDto dto)
        {
            await _roomTypeService.UpdateRoomTypeAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Room type updated successfully." });
        }

        /// <summary>Activate or deactivate a room type.</summary>
        [HttpPatch("{roomTypeId}/status")]
        public async Task<IActionResult> ToggleStatus(Guid roomTypeId, [FromQuery] bool isActive)
        {
            await _roomTypeService.ToggleRoomTypeStatusAsync(GetUserId(), roomTypeId, isActive);
            return Ok(new { success = true, message = "Room type status updated." });
        }

        /// <summary>Add a date-range pricing rate. Throws on overlapping ranges.</summary>
        [HttpPost("rate")]
        public async Task<IActionResult> AddRate([FromBody] CreateRoomTypeRateDto dto)
        {
            await _roomTypeService.AddRateAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Rate added successfully." });
        }

        /// <summary>Update an existing pricing rate.</summary>
        [HttpPut("rate")]
        public async Task<IActionResult> UpdateRate([FromBody] UpdateRoomTypeRateDto dto)
        {
            await _roomTypeService.UpdateRateAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Rate updated successfully." });
        }

        /// <summary>Returns the applicable rate for a specific date.</summary>
        [HttpPost("rate-by-date")]
        public async Task<IActionResult> GetRateByDate([FromBody] GetRateByDateRequestDto dto)
        {
            var rate = await _roomTypeService.GetRateByDateAsync(GetUserId(), dto);
            return Ok(new { success = true, data = rate });
        }

        /// <summary>Returns all pricing rates for a room type ordered by start date.</summary>
        [HttpGet("{roomTypeId}/rates")]
        public async Task<IActionResult> GetRates(Guid roomTypeId)
        {
            var rates = await _roomTypeService.GetRatesAsync(GetUserId(), roomTypeId);
            return Ok(new { success = true, data = rates });
        }
    }
}
