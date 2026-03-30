namespace HotelBookingAppWebApi.Models.DTOs.AuditLog
{
    /// <summary>Single audit log entry returned to the client.</summary>
    public class AuditLogResponseDto
    {
        public Guid AuditLogId { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string Changes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Paged wrapper for audit log entries.</summary>
    public class PagedAuditLogResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<AuditLogResponseDto> Logs { get; set; } = new List<AuditLogResponseDto>();
    }
}
