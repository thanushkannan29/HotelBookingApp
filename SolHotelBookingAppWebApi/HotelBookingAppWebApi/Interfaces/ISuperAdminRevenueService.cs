using HotelBookingAppWebApi.Models.DTOs.Revenue;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Records and retrieves the 2% platform commission earned by SuperAdmin on completed reservations.</summary>
    public interface ISuperAdminRevenueService
    {
        /// <summary>
        /// Records a 2% commission for a completed reservation.
        /// Idempotent — safe to call multiple times for the same reservation.
        /// </summary>
        Task RecordCommissionAsync(Guid reservationId);

        /// <summary>Returns paged commission records ordered by most recent.</summary>
        Task<PagedRevenueResponseDto> GetAllRevenueAsync(int page, int pageSize);

        /// <summary>Returns the total commission earned to date.</summary>
        Task<RevenueSummaryDto> GetSummaryAsync();
    }
}
