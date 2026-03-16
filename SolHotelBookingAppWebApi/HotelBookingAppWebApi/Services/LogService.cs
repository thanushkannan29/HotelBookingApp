using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Log;
using Microsoft.EntityFrameworkCore;
namespace HotelBookingAppWebApi.Services
{
    public class LogService : ILogService
    {
        private readonly HotelBookingContext _context;

        public LogService(HotelBookingContext context)
        {
            _context = context;
        }

       

         
        // GET ALL LOGS (ADMIN)
         
        public async Task<PagedLogResponseDto> GetAllLogsAsync(
            int page,
            int pageSize)
        {
            var query = _context.Logs
                .OrderByDescending(l => l.CreatedAt);

            var total = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => MapToDto(l))
                .ToListAsync();

            return new PagedLogResponseDto
            {
                TotalCount = total,
                Logs = logs
            };
        }

         
        // GET USER LOGS
         
        public async Task<PagedLogResponseDto> GetUserLogsAsync(
            Guid userId,
            int page,
            int pageSize)
        {
            var query = _context.Logs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt);

            var total = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => MapToDto(l))
                .ToListAsync();

            return new PagedLogResponseDto
            {
                TotalCount = total,
                Logs = logs
            };
        }

        private static LogResponseDto MapToDto(Log l)
        {
            return new LogResponseDto
            {
                LogId = l.LogId,
                Message = l.Message,
                ErrorNumber = l.ErrorCode,
                Role = l.Role,
                UserName = l.UserName,
                CreatedAt = l.CreatedAt
            };
        }
    }
}
