using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin hotel management — update details, toggle status, and set GST.</summary>
    [Route("api/admin/hotels")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminHotelController : ControllerBase
    {
        private readonly IHotelService _hotelService;

        public AdminHotelController(IHotelService hotelService)
            => _hotelService = hotelService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Update hotel name, address, city, description, contact, and UPI ID.</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateHotelDto dto)
        {
            await _hotelService.UpdateHotelAsync(GetUserId(), dto);
            return Ok(new { success = true, message = "Hotel updated successfully." });
        }

        /// <summary>Activate or deactivate the hotel.</summary>
        [HttpPatch("status")]
        public async Task<IActionResult> ToggleStatus([FromQuery] bool isActive)
        {
            await _hotelService.ToggleHotelStatusAsync(GetUserId(), isActive);
            return Ok(new { success = true, message = "Hotel status updated successfully." });
        }

        /// <summary>Update the hotel's GST percentage (0–28).</summary>
        [HttpPatch("gst")]
        public async Task<IActionResult> UpdateGst([FromBody] UpdateHotelGstDto dto)
        {
            await _hotelService.UpdateHotelGstAsync(GetUserId(), dto.GstPercent);
            return Ok(new { success = true, message = "GST updated successfully." });
        }
    }
}
