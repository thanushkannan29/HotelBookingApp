using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using HotelBookingAppWebApi.Models.StaticData;
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

    // ── PUBLIC CITY ───────────────────────────────────────────────────────────
    [Route("api/public/cities")]
    [ApiController]
    public class PublicCityController : ControllerBase
    {
        /// <summary>Search Indian cities by name (no auth) — min 2 chars</summary>
        [HttpGet("search")]
        public IActionResult Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new { success = true, data = Array.Empty<object>() });

            var result = IndianCities.Search(query);
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