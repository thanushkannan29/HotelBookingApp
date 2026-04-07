using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin review management — view hotel reviews and post replies.</summary>
    [Route("api/admin/reviews")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public AdminReviewController(IReviewService reviewService)
            => _reviewService = reviewService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Returns paged reviews for the hotel with optional rating filter and sort.</summary>
        [HttpPost]
        public async Task<IActionResult> GetHotelReviews([FromBody] GetHotelReviewsRequestDto dto)
        {
            var result = await _reviewService.GetAdminHotelReviewsAsync(
                GetUserId(), dto.Page, dto.PageSize, dto.MinRating, dto.MaxRating, dto.SortDir);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Post an admin reply to a guest review.</summary>
        [HttpPatch("{reviewId}/reply")]
        public async Task<IActionResult> Reply(Guid reviewId, [FromBody] ReplyToReviewDto dto)
        {
            await _reviewService.ReplyToReviewAsync(GetUserId(), reviewId, dto.AdminReply);
            return Ok(new { success = true, message = "Reply saved." });
        }
    }
}
