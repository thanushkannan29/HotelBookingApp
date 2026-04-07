using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.Admin
{
    /// <summary>Admin transaction management — mark failed payments so guests can retry.</summary>
    [Route("api/admin/transactions")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminTransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public AdminTransactionController(ITransactionService transactionService)
            => _transactionService = transactionService;

        private Guid GetUserId()
            => Guid.Parse(User.FindFirstValue("nameid")!);

        /// <summary>
        /// Marks a transaction as Failed and resets the reservation to Pending
        /// so the guest can attempt payment again.
        /// </summary>
        [HttpPatch("{transactionId}/mark-failed")]
        public async Task<IActionResult> MarkFailed(Guid transactionId)
        {
            await _transactionService.MarkTransactionFailedAsync(transactionId, GetUserId());
            return Ok(new { success = true, message = "Transaction marked as failed. Reservation reset to Pending." });
        }
    }
}
