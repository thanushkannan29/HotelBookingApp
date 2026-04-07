using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers
{
    /// <summary>Guest review lifecycle — add, update, delete, and public hotel review queries.</summary>
    [Route("api/reviews")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
            => _reviewService = reviewService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Add a review for a completed reservation. Credits ₹100 wallet reward.</summary>
        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDto dto)
        {
            var result = await _reviewService.AddReviewAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Update rating, comment, or image for an existing review.</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewDto dto)
        {
            var result = await _reviewService.UpdateReviewAsync(GetUserId(), id, dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Delete a review and reverse the ₹100 wallet reward.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            await _reviewService.DeleteReviewAsync(GetUserId(), id);
            return Ok(new { success = true, message = "Review deleted successfully." });
        }

        /// <summary>Returns paged public reviews for a hotel. No authentication required.</summary>
        [HttpPost("hotel")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByHotel([FromBody] GetHotelReviewsRequestDto dto)
        {
            var result = await _reviewService.GetReviewsByHotelAsync(dto.HotelId, dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns paged reviews submitted by the authenticated guest.</summary>
        [HttpPost("my-reviews/paged")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetMyReviewsPaged([FromBody] PageQueryDto dto)
        {
            var result = await _reviewService.GetMyReviewsPagedAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }
    }
}
