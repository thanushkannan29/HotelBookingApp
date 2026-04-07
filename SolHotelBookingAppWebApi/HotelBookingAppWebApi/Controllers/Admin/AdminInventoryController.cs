using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin inventory management — add date ranges, update totals, and query availability.</summary>
    [Route("api/admin/inventory")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public AdminInventoryController(IInventoryService inventoryService)
            => _inventoryService = inventoryService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Add inventory records for a date range. Skips existing dates (idempotent).</summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateInventoryDto dto)
        {
            await _inventoryService.AddInventoryAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Inventory added successfully." });
        }

        /// <summary>Update total inventory for a single date slot.</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateInventoryDto dto)
        {
            await _inventoryService.UpdateInventoryAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Inventory updated successfully." });
        }

        /// <summary>Returns inventory records for a room type between two dates.</summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] Guid roomTypeId,
            [FromQuery] DateOnly start,
            [FromQuery] DateOnly end)
        {
            var data = await _inventoryService.GetInventoryAsync(GetUserId(), roomTypeId, start, end);
            return Ok(new { success = true, data });
        }
    }
}
