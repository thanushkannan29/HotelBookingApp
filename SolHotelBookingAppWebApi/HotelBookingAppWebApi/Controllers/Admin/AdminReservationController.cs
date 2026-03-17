using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    [Route("api/admin/reservations")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminReservationController : ControllerBase
    {
        private readonly IReservationService _service;

        public AdminReservationController(IReservationService service)
        {
            _service = service;
        }

        [HttpPatch("{code}/complete")]
        public async Task<IActionResult> Complete(string code)
        {
            await _service.CompleteReservationAsync(code);
            return Ok(new { message = "Reservation marked as completed" });
        }
    }

}
