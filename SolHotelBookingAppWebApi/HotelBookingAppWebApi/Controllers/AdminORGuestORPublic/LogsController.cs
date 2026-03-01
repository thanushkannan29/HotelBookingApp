using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Controllers.AdminORGuestORPublic
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LogsController : ControllerBase
    {
        private readonly ILogService _service;

        public LogsController(ILogService service)
        {
            _service = service;
        }

        private Guid GetUserId()
        {
            return Guid.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        

         
        // GET OWN LOGS
         
        [HttpGet("my-logs")]
        public async Task<IActionResult> GetMyLogs(int page = 1, int pageSize = 10)
        {
            var result = await _service
                .GetUserLogsAsync(GetUserId(), page, pageSize);

            return Ok(result);
        }

         
        // GET ALL LOGS (ADMIN for now need to add super admin role in user modles)
         
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10)
        {
            var result = await _service
                .GetAllLogsAsync(page, pageSize);

            return Ok(result);
        }
    }
}
