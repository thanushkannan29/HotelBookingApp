using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    // ── GUEST RESERVATIONS ────────────────────────────────────────────────────
    [Route("api/guest/reservations")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestReservationController : ControllerBase
    {
        private readonly IReservationService _service;
        public GuestReservationController(IReservationService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Create a new reservation (supports room selection)</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
        {
            var result = await _service.CreateReservationAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Get a single reservation by code</summary>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var result = await _service.GetReservationByCodeAsync(GetUserId(), code);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Get all reservations (no pagination)</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var result = await _service.GetMyReservationsAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Get reservation history with pagination</summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetMyReservationsPagedAsync(GetUserId(), page, pageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Cancel a reservation (creates a refund request if payment was made)</summary>
        [HttpPatch("{code}/cancel")]
        public async Task<IActionResult> Cancel(string code, [FromBody] CancelReservationDto dto)
        {
            await _service.CancelReservationAsync(GetUserId(), code, dto.Reason);
            return Ok(new { success = true, message = "Reservation cancelled successfully." });
        }

        /// <summary>Get available rooms for a specific hotel and room type</summary>
        [HttpGet("available-rooms")]
        public async Task<IActionResult> GetAvailableRooms(
            [FromQuery] Guid hotelId,
            [FromQuery] Guid roomTypeId,
            [FromQuery] DateOnly checkIn,
            [FromQuery] DateOnly checkOut)
        {
            var result = await _service.GetAvailableRoomsAsync(hotelId, roomTypeId, checkIn, checkOut);
            return Ok(new { success = true, data = result });
        }
    }

    // ── GUEST REFUND REQUESTS ─────────────────────────────────────────────────
    [Route("api/guest/refund-requests")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestRefundRequestController : ControllerBase
    {
        private readonly IRefundRequestService _service;
        public GuestRefundRequestController(IRefundRequestService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Get all refund requests for the logged-in guest</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var result = await _service.GetGuestRefundRequestsAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }
    }
}
