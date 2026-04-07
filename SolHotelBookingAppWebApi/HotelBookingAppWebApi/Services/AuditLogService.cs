using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Records and retrieves audit log entries for admin and SuperAdmin views.
    /// Uses a compiled EF projection expression to avoid repeated allocations.
    /// </summary>
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

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task LogAsync(
            Guid? userId, string action, string entityName, Guid? entityId, string changes)
        {
            await _auditRepo.AddAsync(BuildLogEntry(userId, action, entityName, entityId, changes));
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PagedAuditLogResponseDto> GetAdminAuditLogsAsync(
            Guid adminUserId, int page, int pageSize, string? search = null)
        {
            var hotelId = await GetAdminHotelIdAsync(adminUserId);
            var query = BuildAdminQuery(adminUserId, hotelId, search);
            return await BuildPagedResponseAsync(query, page, pageSize);
        }

        public async Task<PagedAuditLogResponseDto> GetAllAuditLogsAsync(
            int page, int pageSize,
            Guid? hotelId = null, Guid? userId = null,
            string? action = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var query = BuildSuperAdminQuery(hotelId, userId, action, dateFrom, dateTo);
            return await BuildPagedResponseAsync(query, page, pageSize);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<Guid?> GetAdminHotelIdAsync(Guid adminUserId)
            => await _userRepo.GetQueryable()
                .Where(u => u.UserId == adminUserId)
                .Select(u => u.HotelId)
                .FirstOrDefaultAsync();

        private IQueryable<AuditLog> BuildAdminQuery(
            Guid adminUserId, Guid? hotelId, string? search)
        {
            var query = _auditRepo.GetQueryable()
                .Where(al => al.UserId == adminUserId ||
                             (al.EntityId == hotelId && hotelId != null))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(al =>
                    al.Action.Contains(search) ||
                    al.EntityName.Contains(search) ||
                    al.Changes.Contains(search));

            return query.OrderByDescending(al => al.CreatedAt);
        }

        private IQueryable<AuditLog> BuildSuperAdminQuery(
            Guid? hotelId, Guid? userId, string? action,
            DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _auditRepo.GetQueryable().AsQueryable();

            if (userId.HasValue) query = query.Where(al => al.UserId == userId.Value);
            if (hotelId.HasValue) query = query.Where(al => al.EntityId == hotelId.Value);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(al => al.Action.Contains(action));
            if (dateFrom.HasValue) query = query.Where(al => al.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue) query = query.Where(al => al.CreatedAt <= dateTo.Value);

            return query.OrderByDescending(al => al.CreatedAt);
        }

        private static async Task<PagedAuditLogResponseDto> BuildPagedResponseAsync(
            IQueryable<AuditLog> query, int page, int pageSize)
        {
            var total = await query.CountAsync();
            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ProjectToDto)
                .ToListAsync();

            return new PagedAuditLogResponseDto { TotalCount = total, Logs = logs };
        }

        private static AuditLog BuildLogEntry(
            Guid? userId, string action, string entityName, Guid? entityId, string changes) => new()
        {
            AuditLogId = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Changes = changes,
            CreatedAt = DateTime.UtcNow
        };

        private static readonly Expression<Func<AuditLog, AuditLogResponseDto>> ProjectToDto =
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
