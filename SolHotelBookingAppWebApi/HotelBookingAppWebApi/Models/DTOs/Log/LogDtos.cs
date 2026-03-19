namespace HotelBookingAppWebApi.Models.DTOs.Log
{
    public class LogResponseDto
    {
        public Guid LogId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PagedLogResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<LogResponseDto> Logs { get; set; } = new List<LogResponseDto>();
    }
}

namespace HotelBookingAppWebApi.Models.DTOs.AuditLog
{
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

    public class PagedAuditLogResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<AuditLogResponseDto> Logs { get; set; } = new List<AuditLogResponseDto>();
    }
}
