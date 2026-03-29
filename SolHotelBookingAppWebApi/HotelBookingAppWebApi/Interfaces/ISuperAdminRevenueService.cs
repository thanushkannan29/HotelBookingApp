using HotelBookingAppWebApi.Models.DTOs.Revenue;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ISuperAdminRevenueService
    {
        Task<PagedRevenueResponseDto> GetAllRevenueAsync(int page, int pageSize);
        Task<RevenueSummaryDto> GetSummaryAsync();
        Task RecordCommissionAsync(Guid reservationId); // called when reservation is completed
    }
}
