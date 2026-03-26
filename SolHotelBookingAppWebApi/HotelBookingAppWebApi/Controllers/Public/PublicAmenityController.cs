using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.Public
{
    // ── PUBLIC AMENITY ────────────────────────────────────────────────────────
    [Route("api/public/amenities")]
    [ApiController]
    public class PublicAmenityController : ControllerBase
    {
        private readonly IAmenityService _service;

        public PublicAmenityController(IAmenityService service) => _service = service;

        /// <summary>All active amenities — used for filtering UI (no auth)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllActiveAsync();
            return Ok(new { success = true, data = result });
        }

        /// <summary>Search amenities by name (no auth)</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var result = await _service.SearchAsync(query);
            return Ok(new { success = true, data = result });
        }
    }
}

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    // ── SUPERADMIN AMENITY ────────────────────────────────────────────────────
    [Route("api/superadmin/amenities")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminAmenityController : ControllerBase
    {
        private readonly IAmenityService _service;

        public SuperAdminAmenityController(IAmenityService service) => _service = service;

        /// <summary>Create a new amenity</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAmenityDto dto)
        {
            var result = await _service.CreateAmenityAsync(dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Update an existing amenity</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateAmenityDto dto)
        {
            var result = await _service.UpdateAmenityAsync(dto);
            return Ok(new { success = true, data = result });
        }
    }
}