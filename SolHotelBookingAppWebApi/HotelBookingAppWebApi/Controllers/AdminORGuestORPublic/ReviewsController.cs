using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewsController(IReviewService service)
        {
            _service = service;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDto dto)
        {
            var result = await _service.AddReviewAsync(GetUserId(), dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewDto dto)
        {
            var result = await _service.UpdateReviewAsync(GetUserId(), id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            await _service.DeleteReviewAsync(GetUserId(), id);
            return Ok(new { message = "Review deleted successfully." });
        }

        //  Complex filter → POST (Correct)
        [HttpPost("hotel")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByHotel([FromBody] GetHotelReviewsRequestDto dto)
        {
            var result = await _service.GetReviewsByHotelAsync(
                dto.HotelId,
                dto.Page,
                dto.PageSize);

            return Ok(result);
        }


        [HttpGet("my-reviews")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetMyReviews()
        {
            var result = await _service.GetMyReviewsAsync(GetUserId());
            return Ok(result);
        }
    }

}
