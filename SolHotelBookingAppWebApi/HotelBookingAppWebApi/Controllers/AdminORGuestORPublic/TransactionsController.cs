using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _service;

        public TransactionsController(ITransactionService service)
        {
            _service = service;
        }

        
        // CREATE PAYMENT (Guest)
        
        [HttpPost]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreatePayment(CreatePaymentDto dto)
        {
            var result = await _service.CreatePaymentAsync(dto);
            return Ok(result);
        }

        

        
        // REFUND (Admin)
        
        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Refund(Guid id, RefundRequestDto dto)
        {
            var result = await _service.RefundAsync(id, dto);
            return Ok(result);
        }

        
        // PAGINATED LIST (Admin)
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
        {
            var result = await _service.GetAllTransactionsAsync(page, pageSize);
            return Ok(result);
        }
    }
}
