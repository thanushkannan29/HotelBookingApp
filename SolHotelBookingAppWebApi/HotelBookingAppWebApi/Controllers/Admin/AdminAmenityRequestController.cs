using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin amenity requests — submit and view requests for new amenities.</summary>
    [Route("api/admin/amenity-requests")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAmenityRequestController : ControllerBase
    {
        private readonly IAmenityRequestService _amenityRequestService;

        public AdminAmenityRequestController(IAmenityRequestService amenityRequestService)
            => _amenityRequestService = amenityRequestService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Submit a new amenity request to SuperAdmin.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAmenityRequestDto dto)
        {
            var result = await _amenityRequestService.CreateRequestAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns paged amenity requests submitted by this admin.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] AmenityRequestAdminQueryDto dto)
        {
            var result = await _amenityRequestService.GetAdminRequestsPagedAsync(
                GetUserId(), dto.Page, dto.PageSize, dto.Search);
            return Ok(new { success = true, data = result });
        }
    }
}
