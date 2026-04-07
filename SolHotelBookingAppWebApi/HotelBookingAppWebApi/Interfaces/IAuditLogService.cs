using HotelBookingAppWebApi.Models.DTOs.AuditLog;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Records and retrieves structured audit log entries for admin and SuperAdmin views.</summary>
    public interface IAuditLogService
    {
        /// <summary>Persists a single audit log entry. Safe to call from within an existing transaction.</summary>
        Task LogAsync(Guid? userId, string action, string entityName, Guid? entityId, string changes);

        /// <summary>Admin: returns paged audit logs scoped to their user id and hotel.</summary>
        Task<PagedAuditLogResponseDto> GetAdminAuditLogsAsync(
            Guid adminUserId, int page, int pageSize, string? search = null);

        /// <summary>SuperAdmin: returns all audit logs with optional hotel, user, action, and date filters.</summary>
        Task<PagedAuditLogResponseDto> GetAllAuditLogsAsync(
            int page, int pageSize,
            Guid? hotelId = null, Guid? userId = null,
            string? action = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    }
}
