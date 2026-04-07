using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    /// <summary>SuperAdmin amenity catalogue management — CRUD and status toggling.</summary>
    [Route("api/superadmin/amenities")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminAmenityController : ControllerBase
    {
        private readonly IAmenityService _amenityService;

        public SuperAdminAmenityController(IAmenityService amenityService)
            => _amenityService = amenityService;

        /// <summary>Create a new amenity in the catalogue.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAmenityDto dto)
        {
            var result = await _amenityService.CreateAmenityAsync(dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Update an existing amenity.</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateAmenityDto dto)
        {
            var result = await _amenityService.UpdateAmenityAsync(dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns paged amenities including inactive ones.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null)
        {
            var result = await _amenityService.GetAllAmenitiesPagedAsync(page, pageSize, search, category);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Toggle the active/inactive status of an amenity.</summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var isActive = await _amenityService.ToggleAmenityStatusAsync(id);
            return Ok(new { success = true, data = new { isActive } });
        }

        /// <summary>Delete an amenity (only if not in use by any room type).</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _amenityService.DeleteAmenityAsync(id);
            return Ok(new { success = true, message = "Amenity deleted." });
        }
    }
}
