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

        private Guid GetUserId()
        {
            return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // ✅ Create Reservation
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
        {
            var result = await _service.CreateReservationAsync(GetUserId(), dto);

            return CreatedAtAction(nameof(GetByCode),
                new { code = result.ReservationCode },
                result);
        }

        // ✅ Get Reservation By Code
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var result = await _service.GetReservationByCodeAsync(GetUserId(), code);
            return Ok(result);
        }

        // ✅ Get My Reservations
        [HttpGet]
        public async Task<IActionResult> GetMyReservations()
        {
            var result = await _service.GetMyReservationsAsync(GetUserId());
            return Ok(result);
        }

        // ✅ Cancel Reservation
        [HttpPut("{code}/cancel")]
        public async Task<IActionResult> Cancel(string code, [FromBody] CancelReservationDto dto)
        {
            await _service.CancelReservationAsync(GetUserId(), code, dto.Reason);
            return Ok(new { message = "Reservation cancelled successfully" });
        }
    }
}
