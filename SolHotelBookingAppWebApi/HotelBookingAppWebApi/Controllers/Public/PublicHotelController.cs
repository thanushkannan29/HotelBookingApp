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

        [HttpPost("search")]
        public async Task<IActionResult> Search(SearchHotelRequestDto request)
        {
            return Ok(await _service.SearchHotelsAsync(request));
        }

        [HttpGet("{hotelId}")]
        public async Task<IActionResult> GetDetails(Guid hotelId)
        {
            return Ok(await _service.GetHotelDetailsAsync(hotelId));
        }
    }
}
