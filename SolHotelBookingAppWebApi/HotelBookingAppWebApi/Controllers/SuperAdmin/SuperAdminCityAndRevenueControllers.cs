using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using HotelBookingAppWebApi.Models.DTOs.City;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    // ── CITY MANAGEMENT ───────────────────────────────────────────────────────
    [Route("api/superadmin/cities")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminCityController : ControllerBase
    {
        private readonly ICityService _service;
        public SuperAdminCityController(ICityService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var result = await _service.GetAllCitiesPagedAsync(page, pageSize, search);
            return Ok(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateCityDto dto)
        {
            var result = await _service.AddCityAsync(dto);
            return Ok(new { success = true, data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCityDto dto)
        {
            var result = await _service.UpdateCityAsync(id, dto);
            return Ok(new { success = true, data = result });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var isActive = await _service.ToggleCityStatusAsync(id);
            return Ok(new { success = true, data = new { isActive } });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteCityAsync(id);
            return Ok(new { success = true, message = "City deleted." });
        }
    }

    // ── AMENITY REQUESTS ──────────────────────────────────────────────────────
    [Route("api/superadmin/amenity-requests")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminAmenityRequestController : ControllerBase
    {
        private readonly IAmenityRequestService _service;
        public SuperAdminAmenityRequestController(IAmenityRequestService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = "All",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllRequestsAsync(status, page, pageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var result = await _service.ApproveRequestAsync(id, GetUserId());
            return Ok(new { success = true, data = result });
        }

        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAmenityRequestDto dto)
        {
            var result = await _service.RejectRequestAsync(id, GetUserId(), dto.Note);
            return Ok(new { success = true, data = result });
        }
    }

    // ── REVENUE ───────────────────────────────────────────────────────────────
    [Route("api/superadmin/revenue")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminRevenueController : ControllerBase
    {
        private readonly ISuperAdminRevenueService _service;
        public SuperAdminRevenueController(ISuperAdminRevenueService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _service.GetAllRevenueAsync(page, pageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _service.GetSummaryAsync();
            return Ok(new { success = true, data = result });
        }

        [HttpPatch("{id}/mark-sent")]
        public async Task<IActionResult> MarkSent(Guid id)
        {
            await _service.MarkSentAsync(id);
            return Ok(new { success = true, message = "Marked as sent." });
        }
    }
}
