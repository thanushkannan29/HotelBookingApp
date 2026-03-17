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
            var result = await _service.CreateReservationAsync(GetUserId(), dto);

            return CreatedAtAction(nameof(GetByCode),
                new { code = result.ReservationCode },
                result);
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            return Ok(await _service.GetReservationByCodeAsync(GetUserId(), code));
        }

        [HttpGet]
        public async Task<IActionResult> GetMyReservations()
        {
            return Ok(await _service.GetMyReservationsAsync(GetUserId()));
        }

        [HttpPatch("{code}/cancel")]
        public async Task<IActionResult> Cancel(string code, [FromBody] CancelReservationDto dto)
        {
            await _service.CancelReservationAsync(GetUserId(), code, dto.Reason);
            return Ok(new { message = "Reservation cancelled successfully" });
        }
    }

}
