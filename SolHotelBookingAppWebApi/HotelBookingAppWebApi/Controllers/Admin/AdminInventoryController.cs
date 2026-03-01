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
        public async Task<IActionResult> AddInventory(CreateInventoryDto dto)
        {
            await _service.AddInventoryAsync(GetUserId(), dto);
            return Ok("Inventory added successfully");
        }

        [HttpPut]
        public async Task<IActionResult> UpdateInventory(UpdateInventoryDto dto)
        {
            await _service.UpdateInventoryAsync(GetUserId(), dto);
            return Ok("Inventory updated");
        }

        [HttpPatch("adjust")]
        public async Task<IActionResult> Adjust(AdjustReservedInventoryDto dto)
        {
            await _service.AdjustReservedInventoryAsync(GetUserId(), dto);
            return Ok("Reserved inventory updated");
        }

        [HttpGet]
        public async Task<IActionResult> GetInventory(
            Guid roomTypeId,
            DateOnly start,
            DateOnly end)
        {
            var data = await _service.GetInventoryAsync(GetUserId(), roomTypeId, start, end);
            return Ok(data);
        }
    }
}
