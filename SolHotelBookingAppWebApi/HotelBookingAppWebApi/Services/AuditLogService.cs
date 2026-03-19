using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IRepository<Guid, AuditLog> _auditRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(
            IRepository<Guid, AuditLog> auditRepo,
            IRepository<Guid, User> userRepo,
            IUnitOfWork unitOfWork)
        {
            _auditRepo = auditRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        // ── LOG AN ACTION ─────────────────────────────────────────────────────
        public async Task LogAsync(Guid? userId, string action, string entityName, Guid? entityId, string changes)
        {
            var log = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Changes = changes,
                CreatedAt = DateTime.UtcNow
            };

            await _auditRepo.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        // ── ADMIN: LOGS FOR THEIR HOTEL ───────────────────────────────────────
        public async Task<PagedAuditLogResponseDto> GetAdminAuditLogsAsync(Guid adminUserId, int page, int pageSize)
        {
            var hotelId = await _userRepo.GetQueryable()
                .Where(u => u.UserId == adminUserId)
                .Select(u => u.HotelId)
                .FirstOrDefaultAsync();

            // Filter to actions involving Hotel, RoomType, Room entities matching the admin's hotel
            var query = _auditRepo.GetQueryable()
                .Where(al => al.UserId == adminUserId ||
                             (al.EntityId == hotelId && hotelId != null))
                .OrderByDescending(al => al.CreatedAt);

            var total = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapSelector)
                .ToListAsync();

            return new PagedAuditLogResponseDto { TotalCount = total, Logs = logs };
        }

        // ── SUPERADMIN: ALL LOGS ──────────────────────────────────────────────
        public async Task<PagedAuditLogResponseDto> GetAllAuditLogsAsync(int page, int pageSize)
        {
            var query = _auditRepo.GetQueryable()
                .OrderByDescending(al => al.CreatedAt);

            var total = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapSelector)
                .ToListAsync();

            return new PagedAuditLogResponseDto { TotalCount = total, Logs = logs };
        }

        private static readonly System.Linq.Expressions.Expression<Func<AuditLog, AuditLogResponseDto>> MapSelector =
            al => new AuditLogResponseDto
            {
                AuditLogId = al.AuditLogId,
                UserId = al.UserId,
                Action = al.Action,
                EntityName = al.EntityName,
                EntityId = al.EntityId,
                Changes = al.Changes,
                CreatedAt = al.CreatedAt
            };
    }
}
