using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.Public
{
    [Route("api/public/hotels")]
    [ApiController]
    public class PublicHotelController : ControllerBase
    {
        private readonly IHotelService _service;

        public PublicHotelController(IHotelService service)
        {
            _service = service;
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopHotels()
        {
            try
            {
                var result = await _service.GetTopHotelsAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchHotelRequestDto request)
        {
            try
            {
                var result = await _service.SearchHotelsAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{hotelId}")]
        public async Task<IActionResult> GetDetails(Guid hotelId)
        {
            try
            {
                var result = await _service.GetHotelDetailsAsync(hotelId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{hotelId}/roomtypes")]
        public async Task<IActionResult> GetRoomTypes(Guid hotelId)
        {
            try
            {
                var result = await _service.GetRoomTypesAsync(hotelId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{hotelId}/availability")]
        public async Task<IActionResult> GetAvailability(Guid hotelId, DateOnly checkIn, DateOnly checkOut)
        {
            try
            {
                var result = await _service.GetAvailabilityAsync(hotelId, checkIn, checkOut);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }


}
