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
            try
            {
                var result = await _service.AddReviewAsync(GetUserId(), dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewDto dto)
        {
            try
            {
                var result = await _service.UpdateReviewAsync(GetUserId(), id, dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            try
            {
                await _service.DeleteReviewAsync(GetUserId(), id);
                return Ok(new { success = true, message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("hotel")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByHotel([FromBody] GetHotelReviewsRequestDto dto)
        {
            try
            {
                var result = await _service.GetReviewsByHotelAsync(dto.HotelId, dto.Page, dto.PageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("my-reviews")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var result = await _service.GetMyReviewsAsync(GetUserId());
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }


}
