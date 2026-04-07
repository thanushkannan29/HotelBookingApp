namespace HotelBookingAppWebApi.Controllers
{
    /// <summary>Base pagination query used across all paged endpoints.</summary>
    public class PageQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int PageNumber => Page;
    }

    /// <summary>Pagination + search for log endpoints.</summary>
    public class LogQueryDto : PageQueryDto
    {
        public string? Search { get; set; }
    }

    /// <summary>Pagination + sort for transaction list endpoints.</summary>
    public class TransactionQueryDto : PageQueryDto
    {
        public string? SortField { get; set; }
        public string? SortDir { get; set; }
    }

    /// <summary>Pagination + filters for reservation list endpoints.</summary>
    public class ReservationQueryDto : PageQueryDto
    {
        public string? Status { get; set; } = "All";
        public string? Search { get; set; }
        public string? SortField { get; set; }
        public string? SortDir { get; set; }
    }

    /// <summary>Pagination + search for admin audit log endpoint.</summary>
    public class AuditLogQueryDto : PageQueryDto
    {
        public string? Search { get; set; }
    }

    /// <summary>Pagination + filters for SuperAdmin audit log endpoint.</summary>
    public class AuditLogSuperAdminQueryDto : PageQueryDto
    {
        public string? HotelId { get; set; }
        public string? UserId { get; set; }
        public string? Action { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }

    /// <summary>Pagination + search for admin amenity request list.</summary>
    public class AmenityRequestAdminQueryDto : PageQueryDto
    {
        public string? Search { get; set; }
    }

    /// <summary>Pagination + status filter for SuperAdmin amenity request list.</summary>
    public class AmenityRequestQueryDto : PageQueryDto
    {
        public string? Status { get; set; } = "All";
    }

    /// <summary>Pagination for revenue list.</summary>
    public class RevenueQueryDto : PageQueryDto { }

    /// <summary>Pagination + search + status for hotel list (SuperAdmin).</summary>
    public class HotelQueryDto : PageQueryDto
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>Pagination + status + role + search for support request list.</summary>
    public class SupportQueryDto : PageQueryDto
    {
        public string? Status { get; set; }
        public string? Role { get; set; }
        public string? Search { get; set; }
    }

    /// <summary>Pagination + status filter for promo code list.</summary>
    public class PromoQueryDto : PageQueryDto
    {
        public string? Status { get; set; }
    }

    /// <summary>Pagination + status filter for reservation history.</summary>
    public class ReservationHistoryQueryDto : PageQueryDto
    {
        public string? Status { get; set; }
        public string? Search { get; set; }
    }
}
