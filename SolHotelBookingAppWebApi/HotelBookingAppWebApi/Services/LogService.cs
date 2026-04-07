using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Log;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Provides paginated access to application error/request logs.
    /// SuperAdmin sees all logs; authenticated users see only their own.
    /// </summary>
    public class LogService : ILogService
    {
        private readonly IRepository<Guid, Log> _logRepo;

        public LogService(IRepository<Guid, Log> logRepo)
        {
            _logRepo = logRepo;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<PagedLogResponseDto> GetAllLogsAsync(
            int page, int pageSize, string? search = null)
        {
            ValidatePagination(page, pageSize);
            var query = BuildSearchQuery(search);
            return await BuildPagedResponseAsync(query, page, pageSize);
        }

        public async Task<PagedLogResponseDto> GetUserLogsAsync(
            Guid userId, int page, int pageSize)
        {
            ValidatePagination(page, pageSize);
            var query = _logRepo.GetQueryable()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt);
            return await BuildPagedResponseAsync(query, page, pageSize);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private static void ValidatePagination(int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
                throw new AppException("Invalid pagination parameters.", 400);
        }

        private IQueryable<Log> BuildSearchQuery(string? search)
        {
            var query = _logRepo.GetQueryable().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l =>
                    l.RequestPath.Contains(search) ||
                    l.ExceptionType.Contains(search) ||
                    l.UserName.Contains(search) ||
                    l.Message.Contains(search));
            return query.OrderByDescending(l => l.CreatedAt);
        }

        private static async Task<PagedLogResponseDto> BuildPagedResponseAsync(
            IQueryable<Log> query, int page, int pageSize)
        {
            var total = await query.CountAsync();
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ProjectToDto)
                .ToListAsync();
            return new PagedLogResponseDto { TotalCount = total, Logs = logs };
        }

        private static readonly Expression<Func<Log, LogResponseDto>> ProjectToDto =
            l => new LogResponseDto
            {
                LogId = l.LogId,
                Message = l.Message,
                ExceptionType = l.ExceptionType,
                StackTrace = l.StackTrace,
                StatusCode = l.StatusCode,
                UserName = l.UserName,
                Role = l.Role,
                UserId = l.UserId,
                Controller = l.Controller,
                Action = l.Action,
                HttpMethod = l.HttpMethod,
                RequestPath = l.RequestPath,
                CreatedAt = l.CreatedAt
            };
    }
}
