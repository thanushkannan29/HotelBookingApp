using HotelBookingAppWebApi.Models.DTOs.AuditLog;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(Guid? userId, string action, string entityName, Guid? entityId, string changes);
        Task<PagedAuditLogResponseDto> GetAdminAuditLogsAsync(Guid adminUserId, int page, int pageSize);
        Task<PagedAuditLogResponseDto> GetAllAuditLogsAsync(int page, int pageSize);
    }
}
