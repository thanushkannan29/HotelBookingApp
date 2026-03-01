using HotelBookingAppWebApi.Models.DTOs.Log;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ILogService
    {
        Task<LogResponseDto> CreateLogAsync(Guid userId, CreateLogDto dto);
        Task<PagedLogResponseDto> GetAllLogsAsync(int page, int pageSize);
        Task<PagedLogResponseDto> GetUserLogsAsync(Guid userId, int page, int pageSize);
    }
}
