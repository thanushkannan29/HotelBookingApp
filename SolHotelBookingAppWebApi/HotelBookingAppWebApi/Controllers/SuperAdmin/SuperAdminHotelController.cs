using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.SuperAdmin
{
    /// <summary>SuperAdmin hotel management — list, block, and unblock hotels.</summary>
    [Route("api/superadmin/hotels")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminHotelController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public SuperAdminHotelController(IHotelService hotelService)
            => _hotelService = hotelService;

        /// <summary>Returns paged list of all hotels with revenue and reservation stats.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] HotelQueryDto dto)
        {
            var result = await _hotelService.GetAllHotelsForSuperAdminPagedAsync(
                dto.Page, dto.PageSize, dto.Search, dto.Status);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Block a hotel — prevents the admin from activating it.</summary>
        [HttpPatch("{id}/block")]
        public async Task<IActionResult> Block(Guid id)
        {
            await _hotelService.BlockHotelAsync(id);
            return Ok(new { success = true, message = "Hotel has been blocked." });
        }

        /// <summary>Unblock a hotel — admin can now reactivate it.</summary>
        [HttpPatch("{id}/unblock")]
        public async Task<IActionResult> Unblock(Guid id)
        {
            await _hotelService.UnblockHotelAsync(id);
            return Ok(new { success = true, message = "Hotel has been unblocked." });
        }
    }
}
