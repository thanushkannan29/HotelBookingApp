using HotelBookingAppWebApi.Models.DTOs.Log;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Application error/request log queries.</summary>
    public interface ILogService
    {
        /// <summary>SuperAdmin: returns all logs paged with optional keyword search.</summary>
        Task<PagedLogResponseDto> GetAllLogsAsync(int page, int pageSize, string? search = null);

        /// <summary>Any authenticated user: returns their own logs paged.</summary>
        Task<PagedLogResponseDto> GetUserLogsAsync(Guid userId, int page, int pageSize);
    }
}
