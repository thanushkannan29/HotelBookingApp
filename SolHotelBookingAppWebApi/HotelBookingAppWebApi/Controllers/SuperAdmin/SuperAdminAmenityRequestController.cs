using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    /// <summary>SuperAdmin amenity request management — list, approve, and reject requests.</summary>
    [Route("api/superadmin/amenity-requests")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminAmenityRequestController : ControllerBase
    {
        private readonly IAmenityRequestService _amenityRequestService;

        public SuperAdminAmenityRequestController(IAmenityRequestService amenityRequestService)
            => _amenityRequestService = amenityRequestService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged amenity requests with optional status filter.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] AmenityRequestQueryDto dto)
        {
            var result = await _amenityRequestService.GetAllRequestsAsync(
                dto.Status ?? "All", dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Approve a pending amenity request and add it to the amenity catalogue.</summary>
        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var result = await _amenityRequestService.ApproveRequestAsync(id, GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Reject a pending amenity request with a note.</summary>
        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAmenityRequestDto dto)
        {
            var result = await _amenityRequestService.RejectRequestAsync(id, GetUserId(), dto.Note);
            return Ok(new { success = true, data = result });
        }
    }
}
