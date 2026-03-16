using HotelBookingAppWebApi.Models.DTOs.Dashboard;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId);

        Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId);

        Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync();
    }
}
