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
            return Ok(await _service.GetTopHotelsAsync());
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchHotelRequestDto request)
        {
            return Ok(await _service.SearchHotelsAsync(request));
        }

        [HttpGet("{hotelId}")]
        public async Task<IActionResult> GetDetails(Guid hotelId)
        {
            return Ok(await _service.GetHotelDetailsAsync(hotelId));
        }

        [HttpGet("{hotelId}/roomtypes")]
        public async Task<IActionResult> GetRoomTypes(Guid hotelId)
        {
            return Ok(await _service.GetRoomTypesAsync(hotelId));
        }

        [HttpGet("{hotelId}/availability")]
        public async Task<IActionResult> GetAvailability(
            Guid hotelId,
            [FromQuery] DateOnly checkIn,
            [FromQuery] DateOnly checkOut)
        {
            return Ok(await _service.GetAvailabilityAsync(hotelId, checkIn, checkOut));
        }
    }

}
