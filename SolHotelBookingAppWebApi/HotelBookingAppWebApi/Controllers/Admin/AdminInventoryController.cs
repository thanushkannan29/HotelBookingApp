using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/inventory")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : ControllerBase
    {
        private readonly IInventoryService _service;

        public AdminInventoryController(IInventoryService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> AddInventory([FromBody] CreateInventoryDto dto)
        {
            try
            {
                await _service.AddInventoryAsync(GetUserId(), dto);

                return Ok(new
                {
                    success = true,
                    message = "Inventory added successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateInventory([FromBody] UpdateInventoryDto dto)
        {
            try
            {
                await _service.UpdateInventoryAsync(GetUserId(), dto);

                return Ok(new
                {
                    success = true,
                    message = "Inventory updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInventory(
            [FromQuery] Guid roomTypeId,
            [FromQuery] DateOnly start,
            [FromQuery] DateOnly end)
        {
            try
            {
                var data = await _service.GetInventoryAsync(GetUserId(), roomTypeId, start, end);

                return Ok(new
                {
                    success = true,
                    data
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

}


