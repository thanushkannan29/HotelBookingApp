using HotelBookingAppWebApi.Models.DTOs.RefundRequest;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IRefundRequestService
    {
        /// <summary>Called internally when a guest cancels a confirmed reservation</summary>
        Task CreateRefundRequestAsync(Guid reservationId, Guid userId, string reason);

        /// <summary>Admin: approve a pending refund request → triggers actual refund</summary>
        Task<RefundRequestResponseDto> ApproveRefundAsync(Guid refundRequestId, Guid adminId, string adminResponse);

        /// <summary>Admin: reject a pending refund request</summary>
        Task<RefundRequestResponseDto> RejectRefundAsync(Guid refundRequestId, Guid adminId, string adminResponse);

        /// <summary>Admin: list all refund requests for the admin's hotel</summary>
        Task<IEnumerable<RefundRequestResponseDto>> GetHotelRefundRequestsAsync(Guid adminUserId);

        /// <summary>Guest: list own refund requests</summary>
        Task<IEnumerable<RefundRequestResponseDto>> GetGuestRefundRequestsAsync(Guid userId);
    }
}
