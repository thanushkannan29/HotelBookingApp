using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    [Route("api/guest/reservations")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestReservationController : ControllerBase
    {
        private readonly IReservationService _service;

        public GuestReservationController(IReservationService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
        {
            try
            {
                var result = await _service.CreateReservationAsync(GetUserId(), dto);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            try
            {
                var result = await _service.GetReservationByCodeAsync(GetUserId(), code);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyReservations()
        {
            try
            {
                var result = await _service.GetMyReservationsAsync(GetUserId());
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{code}/cancel")]
        public async Task<IActionResult> Cancel(string code, [FromBody] CancelReservationDto dto)
        {
            try
            {
                await _service.CancelReservationAsync(GetUserId(), code, dto.Reason);

                return Ok(new { success = true, message = "Reservation cancelled successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }


}
