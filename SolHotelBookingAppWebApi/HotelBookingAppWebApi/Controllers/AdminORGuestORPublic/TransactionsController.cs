using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/transactions")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _service;

        public TransactionsController(ITransactionService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            try
            {
                var result = await _service.CreatePaymentAsync(dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequestDto dto)
        {
            try
            {
                var result = await _service.RefundAsync(id, dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Guest")]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var role = User.FindFirstValue(ClaimTypes.Role)!;

                var result = await _service.GetAllTransactionsAsync(userId, role, page, pageSize);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


    }


}
