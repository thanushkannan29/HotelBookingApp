using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin reservation management — list, confirm, and complete reservations.</summary>
    [Route("api/admin/reservations")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public AdminReservationController(IReservationService reservationService)
            => _reservationService = reservationService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged reservations for the hotel with optional status, search, and sort.</summary>
        [HttpPost("list")]
        public async Task<IActionResult> GetList([FromBody] ReservationQueryDto dto)
        {
            var result = await _reservationService.GetAdminReservationsAsync(
                GetUserId(), dto.Status ?? "All", dto.Search,
                dto.Page, dto.PageSize, dto.SortField, dto.SortDir);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Mark a confirmed reservation as completed and trigger commission recording.</summary>
        [HttpPatch("{code}/complete")]
        public async Task<IActionResult> Complete(string code)
        {
            await _reservationService.CompleteReservationAsync(code);
            return Ok(new { success = true, message = "Reservation marked as completed." });
        }

        /// <summary>Confirm a pending reservation.</summary>
        [HttpPatch("{code}/confirm")]
        public async Task<IActionResult> Confirm(string code)
        {
            await _reservationService.ConfirmReservationAsync(code);
            return Ok(new { success = true, message = "Reservation confirmed." });
        }
    }
}
