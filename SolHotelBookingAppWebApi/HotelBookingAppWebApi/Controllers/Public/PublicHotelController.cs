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
                return Ok(await _service.GetTopHotelsAsync());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search(SearchHotelRequestDto request)
        {
            try
            {
                return Ok(await _service.SearchHotelsAsync(request));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{hotelId}")]
        public async Task<IActionResult> GetDetails(Guid hotelId)
        {
            try
            {
                return Ok(await _service.GetHotelDetailsAsync(hotelId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{hotelId}/roomtypes")]
        public async Task<IActionResult> GetRoomTypes(Guid hotelId)
        {
            try
            {
                return Ok(await _service.GetRoomTypesAsync(hotelId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{hotelId}/availability")]
        public async Task<IActionResult> GetAvailability(
            Guid hotelId,
            DateOnly checkIn,
            DateOnly checkOut)
        {
            try
            {
                return Ok(await _service.GetAvailabilityAsync(hotelId, checkIn, checkOut));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
