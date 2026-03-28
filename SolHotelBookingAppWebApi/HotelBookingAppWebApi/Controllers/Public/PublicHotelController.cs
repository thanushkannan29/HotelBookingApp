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

        /// <summary>Top 10 hotels by rating</summary>
        [HttpGet("top")]
        public async Task<IActionResult> GetTopHotels()
        {
            var result = await _service.GetTopHotelsAsync();
            return Ok(new { success = true, data = result });
        }

        /// <summary>Search hotels by city and date range</summary>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchHotelRequestDto request)
        {
            var result = await _service.SearchHotelsAsync(request);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Get all available cities</summary>
        [HttpGet("cities")]
        public async Task<IActionResult> GetCities()
        {
            var result = await _service.GetCitiesAsync();
            return Ok(new { success = true, data = result });
        }

        /// <summary>Get hotels by city (lightweight list)</summary>
        [HttpGet("by-city")]
        public async Task<IActionResult> GetByCity([FromQuery] string city)
        {
            var result = await _service.GetHotelsByCityAsync(city);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Full hotel details including room types and recent reviews</summary>
        [HttpGet("{hotelId}/full-details")]
        public async Task<IActionResult> GetFullDetails(Guid hotelId)
        {
            var result = await _service.GetHotelDetailsAsync(hotelId);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Basic hotel details</summary>
        [HttpGet("{hotelId}")]
        public async Task<IActionResult> GetDetails(Guid hotelId)
        {
            var result = await _service.GetHotelDetailsAsync(hotelId);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Room types for a hotel</summary>
        [HttpGet("{hotelId}/roomtypes")]
        public async Task<IActionResult> GetRoomTypes(Guid hotelId)
        {
            var result = await _service.GetRoomTypesAsync(hotelId);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Room availability for a hotel between check-in and check-out</summary>
        [HttpGet("{hotelId}/availability")]
        public async Task<IActionResult> GetAvailability(
            Guid hotelId,
            [FromQuery] DateOnly checkIn,
            [FromQuery] DateOnly checkOut)
        {
            var result = await _service.GetAvailabilityAsync(hotelId, checkIn, checkOut);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Get distinct active states that have hotels</summary>
        [HttpGet("active-states")]
        public async Task<IActionResult> GetActiveStates()
        {
            var result = await _service.GetActiveStatesAsync();
            return Ok(new { success = true, data = result });
        }

        /// <summary>Get hotels by state name</summary>
        [HttpGet("by-state/{stateName}")]
        public async Task<IActionResult> GetByState(string stateName)
        {
            var result = await _service.GetHotelsByStateAsync(stateName);
            return Ok(new { success = true, data = result });
        }
    }
}
