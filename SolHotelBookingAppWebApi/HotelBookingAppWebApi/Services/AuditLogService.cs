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

        // ── SUPERADMIN: ALL LOGS (with filters) ──────────────────────────────
        public async Task<PagedAuditLogResponseDto> GetAllAuditLogsAsync(
            int page, int pageSize,
            Guid? hotelId = null, Guid? userId = null,
            string? action = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var query = _auditRepo.GetQueryable().AsQueryable();

            if (userId.HasValue)
                query = query.Where(al => al.UserId == userId.Value);

            if (hotelId.HasValue)
                query = query.Where(al => al.EntityId == hotelId.Value);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(al => al.Action.Contains(action));

            if (dateFrom.HasValue)
                query = query.Where(al => al.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(al => al.CreatedAt <= dateTo.Value);

            query = query.OrderByDescending(al => al.CreatedAt);

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
