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

        // Complete Reservation
        [HttpPut("{code}/complete")]
        public async Task<IActionResult> Complete(string code)
        {
            try
            {
                await _service.CompleteReservationAsync(code);

                return Ok(new { message = "Reservation marked as completed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        // (Optional) Future: View all reservations
    }
}
