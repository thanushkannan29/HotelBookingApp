using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.Public
{
    [Route("api/public/cities")]
    [ApiController]
    public class PublicCityController : ControllerBase
    {
        private readonly ICityService _service;
        public PublicCityController(ICityService service) => _service = service;

        /// <summary>Autocomplete: returns up to 10 cities matching the search term</summary>
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            var result = await _service.SearchCitiesAsync(search);
            return Ok(new { success = true, data = result });
        }

        /// <summary>All active cities</summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllActiveCitiesAsync();
            return Ok(new { success = true, data = result });
        }
    }
}
