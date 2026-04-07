using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Guest
{
    /// <summary>Guest reservation operations — create, view, cancel, and check available rooms.</summary>
    [Route("api/guest/reservations")]
    [ApiController]
    [Authorize(Roles = "Guest")]
    public class GuestReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public GuestReservationController(IReservationService reservationService)
            => _reservationService = reservationService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Create a new reservation with optional room selection and wallet/promo usage.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
        {
            var result = await _reservationService.CreateReservationAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns full details of a single reservation by its code.</summary>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var result = await _reservationService.GetReservationByCodeAsync(GetUserId(), code);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns all reservations for the guest (no pagination).</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _reservationService.GetMyReservationsAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns paged reservation history with optional status and search filters.</summary>
        [HttpPost("history")]
        public async Task<IActionResult> GetHistory([FromBody] ReservationHistoryQueryDto dto)
        {
            var result = await _reservationService.GetMyReservationsPagedAsync(
                GetUserId(), dto.Page, dto.PageSize, dto.Status, dto.Search);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Cancel a reservation. Issues a wallet refund based on the cancellation policy.</summary>
        [HttpPatch("{code}/cancel")]
        public async Task<IActionResult> Cancel(string code, [FromBody] CancelReservationDto dto)
        {
            await _reservationService.CancelReservationAsync(GetUserId(), code, dto.Reason);
            return Ok(new { success = true, message = "Reservation cancelled successfully." });
        }

        /// <summary>Returns rooms available for a hotel, room type, and date range.</summary>
        [HttpGet("available-rooms")]
        public async Task<IActionResult> GetAvailableRooms(
            [FromQuery] Guid hotelId,
            [FromQuery] Guid roomTypeId,
            [FromQuery] DateOnly checkIn,
            [FromQuery] DateOnly checkOut)
        {
            var result = await _reservationService.GetAvailableRoomsAsync(hotelId, roomTypeId, checkIn, checkOut);
            return Ok(new { success = true, data = result });
        }
    }
}
