using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers
{
    /// <summary>Payment creation, refunds, and transaction history across all roles.</summary>
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
            => _transactionService = transactionService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>Records a successful payment and promotes the reservation to Confirmed.</summary>
        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var result = await _transactionService.CreatePaymentAsync(dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Guest-only direct refund within 30 minutes of payment.</summary>
        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> DirectRefund(Guid id, [FromBody] RefundRequestDto dto)
        {
            var result = await _transactionService.DirectGuestRefundAsync(id, GetUserId(), dto);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Records a failed payment attempt for audit purposes.</summary>
        [HttpPost("{reservationId}/record-failed")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> RecordFailed(Guid reservationId)
        {
            await _transactionService.RecordFailedPaymentAsync(reservationId, GetUserId());
            return Ok(new { success = true, message = "Failed payment recorded." });
        }

        /// <summary>Returns paged transactions. Guest sees own; Admin sees hotel's; SuperAdmin sees all.</summary>
        [HttpPost("list")]
        [Authorize(Roles = "Admin,Guest,SuperAdmin")]
        public async Task<IActionResult> GetList([FromBody] TransactionQueryDto dto)
        {
            var role = User.FindFirstValue("role")!;
            var result = await _transactionService.GetAllTransactionsAsync(
                GetUserId(), role, dto.Page, dto.PageSize, dto.SortField, dto.SortDir);
            return Ok(new { success = true, data = result });
        }

        /// <summary>Returns the hotel's UPI ID and payment reference for a pending reservation.</summary>
        [HttpGet("payment-intent/{reservationId}")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> GetPaymentIntent(Guid reservationId)
        {
            var result = await _transactionService.GetPaymentIntentAsync(reservationId, GetUserId());
            return Ok(new { success = true, data = result });
        }
    }
}
