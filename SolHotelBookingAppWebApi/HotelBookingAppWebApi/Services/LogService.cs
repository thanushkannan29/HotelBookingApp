using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Log;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Services
{
    public class LogService : ILogService
    {
        private readonly IRepository<Guid, Log> _logRepo;
        private readonly IUnitOfWork _unitOfWork;

        public LogService(
            IRepository<Guid, Log> logRepo,
            IUnitOfWork unitOfWork)
        {
            _logRepo = logRepo;
            _unitOfWork = unitOfWork;
        }

        
        // COMMON SELECTOR 
        
        private static readonly Expression<Func<Log, LogResponseDto>> LogSelector =
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

        
        // GET ALL LOGS (ADMIN)
        
        public async Task<PagedLogResponseDto> GetAllLogsAsync(int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
                throw new AppException("Invalid pagination parameters", 400);

            var query = _logRepo.GetQueryable()
                .OrderByDescending(l => l.CreatedAt);

            var total = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(LogSelector)
                .ToListAsync();

            return new PagedLogResponseDto
            {
                TotalCount = total,
                Logs = logs
            };
        }

        
        // GET USER LOGS
        
        public async Task<PagedLogResponseDto> GetUserLogsAsync(Guid userId, int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
                throw new AppException("Invalid pagination parameters", 400);

            var query = _logRepo.GetQueryable()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt);

            var total = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(LogSelector)
                .ToListAsync();

            return new PagedLogResponseDto
            {
                TotalCount = total,
                Logs = logs
            };
        }
    }
}
