using HotelBookingAppWebApi.Models.DTOs.RefundRequest;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IRefundRequestService
    {
        /// <summary>Called internally when a guest cancels a confirmed reservation</summary>
        Task CreateRefundRequestAsync(Guid reservationId, Guid userId, string reason, decimal refundAmount, string refundNote);

        /// <summary>Admin: approve a pending refund request → triggers actual refund</summary>
        Task<RefundRequestResponseDto> ApproveRefundAsync(Guid refundRequestId, Guid adminId, ProcessRefundDto dto);

        /// <summary>Admin: reject a pending refund request</summary>
        Task<RefundRequestResponseDto> RejectRefundAsync(Guid refundRequestId, Guid adminId, string adminResponse);

        /// <summary>Admin: list all refund requests for the admin's hotel (paged)</summary>
        Task<PagedRefundRequestResponseDto> GetHotelRefundRequestsPagedAsync(Guid adminUserId, int page, int pageSize);

        /// <summary>Guest: list own refund requests (paged)</summary>
        Task<PagedRefundRequestResponseDto> GetGuestRefundRequestsPagedAsync(Guid userId, int page, int pageSize);

        // Legacy non-paged kept for backward compat (background services etc.)
        Task<IEnumerable<RefundRequestResponseDto>> GetHotelRefundRequestsAsync(Guid adminUserId);
        Task<IEnumerable<RefundRequestResponseDto>> GetGuestRefundRequestsAsync(Guid userId);
    }
}