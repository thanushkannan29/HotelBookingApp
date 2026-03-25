using HotelBookingAppWebApi.Models.DTOs.Revenue;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ISuperAdminRevenueService
    {
        Task<PagedRevenueResponseDto> GetAllRevenueAsync(int page, int pageSize);
        Task<RevenueSummaryDto> GetSummaryAsync();
        Task<bool> MarkSentAsync(Guid revenueId);
        Task ProcessCompletedReservationsAsync(); // called by background service
    }
}
