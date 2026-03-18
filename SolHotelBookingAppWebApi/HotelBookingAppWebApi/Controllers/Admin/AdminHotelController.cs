using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/hotels")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminHotelController : ControllerBase
    {
        private readonly IHotelService _service;

        public AdminHotelController(IHotelService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        //  UPDATE HOTEL
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateHotelDto dto)
        {
            try
            {
                await _service.UpdateHotelAsync(GetUserId(), dto);

                return Ok(new
                {
                    success = true,
                    message = "Hotel updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPatch("status")]
        public async Task<IActionResult> Toggle([FromQuery] bool isActive)
        {
            try
            {
                await _service.ToggleHotelStatusAsync(GetUserId(), isActive);

                return Ok(new
                {
                    success = true,
                    message = "Hotel status updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}
