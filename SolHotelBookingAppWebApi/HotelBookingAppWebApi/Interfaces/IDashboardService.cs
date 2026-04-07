using HotelBookingAppWebApi.Models.DTOs.Dashboard;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Aggregated statistics for role-specific dashboards.</summary>
    public interface IDashboardService
    {
        /// <summary>Returns hotel stats, reservation counts, revenue, and review summary for the admin's hotel.</summary>
        Task<AdminDashboardDto> GetAdminDashboardAsync(Guid userId);

        /// <summary>Returns booking counts and total spend for the authenticated guest.</summary>
        Task<GuestDashboardDto> GetGuestDashboardAsync(Guid userId);

        /// <summary>Returns platform-wide totals — hotels, users, reservations, revenue, and reviews.</summary>
        Task<SuperAdminDashboardDto> GetSuperAdminDashboardAsync();
    }
}
