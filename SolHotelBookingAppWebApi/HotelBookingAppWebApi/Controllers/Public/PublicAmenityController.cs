using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.Public
{
    /// <summary>Public amenity endpoints — no authentication required.</summary>
    [Route("api/public/amenities")]
    [ApiController]
    public class PublicAmenityController : ControllerBase
    {
        private readonly IAmenityService _amenityService;

        public PublicAmenityController(IAmenityService amenityService)
            => _amenityService = amenityService;

        /// <summary>Returns all active amenities ordered by category then name.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _amenityService.GetAllActiveAsync();
            return Ok(new { success = true, data = result });
        }

        /// <summary>Case-insensitive name search, returns up to 20 results.</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var result = await _amenityService.SearchAsync(query);
            return Ok(new { success = true, data = result });
        }
    }
}
