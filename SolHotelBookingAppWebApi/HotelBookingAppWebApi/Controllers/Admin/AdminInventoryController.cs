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
            await _service.AddInventoryAsync(GetUserId(), dto);
            return Ok("Inventory added successfully");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateInventory([FromBody] UpdateInventoryDto dto)
        {
            await _service.UpdateInventoryAsync(GetUserId(), dto);
            return Ok("Inventory updated");
        }

        [HttpPatch("adjust")]//this is for reception work offine reserve the room for customer with cash
        public async Task<IActionResult> Adjust([FromBody] AdjustReservedInventoryDto dto)
        {
            await _service.AdjustReservedInventoryAsync(GetUserId(), dto);
            return Ok("Reserved inventory updated");
        }

        [HttpGet]
        public async Task<IActionResult> GetInventory(
            [FromQuery] Guid roomTypeId,
            [FromQuery] DateOnly start,
            [FromQuery] DateOnly end)
        {
            var data = await _service.GetInventoryAsync(GetUserId(), roomTypeId, start, end);
            return Ok(data);
        }
    }

}
