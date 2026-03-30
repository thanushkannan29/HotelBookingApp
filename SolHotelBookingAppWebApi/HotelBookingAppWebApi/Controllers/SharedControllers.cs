using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers
{
    // ── SHARED PAGINATION DTOs ────────────────────────────────────────────────
    public class PageQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class TransactionQueryDto : PageQueryDto
    {
        public string? SortField { get; set; }
        public string? SortDir { get; set; }
    }

    public class LogQueryDto : PageQueryDto
    {
        public string? Search { get; set; }
    }

    public class AuditLogSuperAdminQueryDto : PageQueryDto
    {
        public string? HotelId { get; set; }
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }
    // ── DASHBOARD ─────────────────────────────────────────────────────────────
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;
        public DashboardController(IDashboardService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var result = await _service.GetAdminDashboardAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        [HttpGet("guest")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GuestDashboard()
        {
            var result = await _service.GetGuestDashboardAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        [HttpGet("superadmin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SuperAdminDashboard()
        {
            var result = await _service.GetSuperAdminDashboardAsync();
            return Ok(new { success = true, data = result });
        }
    }

    // ── USER PROFILE ──────────────────────────────────────────────────────────
    [Route("api/user-profile")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserService _service;
        public UserProfileController(IUserService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _service.GetProfileAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var result = await _service.UpdateProfileAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("booking-history")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetBookingHistory([FromBody] PaginationDto dto)
        {
            var result = await _service.GetBookingHistoryAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }
    }

    // ── TRANSACTIONS ──────────────────────────────────────────────────────────
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _service;
        public TransactionsController(ITransactionService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>Guest pays for a pending reservation</summary>
        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var result = await _service.CreatePaymentAsync(dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// Guest-only direct refund within 30 minutes of payment.
        /// The UI should hide this button after 30 min (check TransactionDate).
        /// Backend also enforces the 30-min window as a safety net.
        /// After 30 min, guest must cancel reservation → RefundRequest flow instead.
        /// </summary>
        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> DirectRefund(Guid id, [FromBody] RefundRequestDto dto)
        {
            var result = await _service.DirectGuestRefundAsync(id, GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Record a failed Razorpay payment attempt</summary>
        [HttpPost("{reservationId}/record-failed")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> RecordFailed(Guid reservationId)
        {
            await _service.RecordFailedPaymentAsync(reservationId, GetUserId());
            return Ok(new { success = true, message = "Failed payment recorded." });
        }

        /// <summary>Get transactions — Guest sees own, Admin sees hotel's, SuperAdmin sees all</summary>
        [HttpPost("list")]
        [Authorize(Roles = "Admin,Guest,SuperAdmin")]
        public async Task<IActionResult> GetAll([FromBody] TransactionQueryDto dto)
        {
            var userId = GetUserId();
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var result = await _service.GetAllTransactionsAsync(userId, role, dto.Page, dto.PageSize, dto.SortField, dto.SortDir);
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// Correction 7D: Payment Intent endpoint.
        /// Guest calls this before making a UPI payment — returns the hotel's UPI ID,
        /// the amount owed, a payment reference (HTLPAY-{reservationCode}), and hotel name.
        /// This is purely informational; the guest pays externally via UPI.
        /// GET /api/transactions/payment-intent/{reservationId}
        /// Auth: Guest
        /// </summary>
        [HttpGet("payment-intent/{reservationId}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetPaymentIntent(Guid reservationId)
        {
            var result = await _service.GetPaymentIntentAsync(reservationId, GetUserId());
            return Ok(new { success = true, data = result });
        }
    }

    // ── REVIEWS ───────────────────────────────────────────────────────────────
    [Route("api/reviews")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;
        public ReviewsController(IReviewService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDto dto)
        {
            var result = await _service.AddReviewAsync(GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateReviewDto dto)
        {
            var result = await _service.UpdateReviewAsync(GetUserId(), id, dto);
            return Ok(new { success = true, data = result });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            await _service.DeleteReviewAsync(GetUserId(), id);
            return Ok(new { success = true, message = "Review deleted successfully." });
        }

        [HttpPost("hotel")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByHotel([FromBody] GetHotelReviewsRequestDto dto)
        {
            var result = await _service.GetReviewsByHotelAsync(dto.HotelId, dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("my-reviews")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetMyReviews()
        {
            var result = await _service.GetMyReviewsAsync(GetUserId());
            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// Correction 9A: My reviews paged — returns { totalCount, reviews }.
        /// POST /api/reviews/my-reviews/paged
        /// Auth: Guest
        /// </summary>
        [HttpPost("my-reviews/paged")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetMyReviewsPaged([FromBody] PageQueryDto dto)
        {
            var result = await _service.GetMyReviewsPagedAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }
    }

    // ── LOGS ──────────────────────────────────────────────────────────────────
    [Route("api/logs")]
    [ApiController]
    [Authorize]
    public class LogsController : ControllerBase
    {
        private readonly ILogService _service;
        public LogsController(ILogService service) => _service = service;
        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("my-logs")]
        public async Task<IActionResult> GetMyLogs([FromBody] PageQueryDto dto)
        {
            var result = await _service.GetUserLogsAsync(GetUserId(), dto.Page, dto.PageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("list")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAll([FromBody] LogQueryDto dto)
        {
            var result = await _service.GetAllLogsAsync(dto.Page, dto.PageSize, dto.Search);
            return Ok(new { success = true, data = result });
        }
    }
}
