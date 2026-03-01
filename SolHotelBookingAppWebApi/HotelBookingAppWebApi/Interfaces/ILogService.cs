using HotelBookingAppWebApi.Models.DTOs.Log;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ILogService
    {
        
        Task<PagedLogResponseDto> GetAllLogsAsync(int page, int pageSize);
        Task<PagedLogResponseDto> GetUserLogsAsync(Guid userId, int page, int pageSize);
    }
}
