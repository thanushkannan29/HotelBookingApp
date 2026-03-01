using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewsController(IReviewService service)
        {
            _service = service;
        }

        
        // ADD REVIEW (Guest)
        
        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> AddReview(CreateReviewDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _service.AddReviewAsync(userId, dto);
            return Ok(result);
        }

        
        // UPDATE REVIEW (Guest)
        
        [HttpPut("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> UpdateReview(Guid id, UpdateReviewDto dto)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _service.UpdateReviewAsync(userId, id, dto);
            return Ok(result);
        }

        
        // DELETE REVIEW (Guest)
        
        [HttpDelete("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            var userId = Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _service.DeleteReviewAsync(userId, id);
            return Ok(new { message = "Review deleted successfully." });
        }

        
        // GET REVIEWS BY HOTEL (Public)
        
        [HttpGet("hotel/{hotelId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByHotel(
            Guid hotelId,
            int page = 1,
            int pageSize = 10)
        {
            var result = await _service
                .GetReviewsByHotelAsync(hotelId, page, pageSize);

            return Ok(result);
        }
    }
}
