using HotelBookingAppWebApi.Models.DTOs.SupportRequest;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface ISupportRequestService
    {
        /// <summary>Public (unauthenticated) submission</summary>
        Task<SupportRequestResponseDto> CreatePublicRequestAsync(PublicSupportRequestDto dto);

        /// <summary>Guest submission — linked to their account</summary>
        Task<SupportRequestResponseDto> CreateGuestRequestAsync(Guid userId, GuestSupportRequestDto dto);

        /// <summary>Admin (hotel) submission — bug/issue report</summary>
        Task<SupportRequestResponseDto> CreateAdminRequestAsync(Guid adminUserId, AdminSupportRequestDto dto);

        /// <summary>Guest: view own requests (paged)</summary>
        Task<PagedSupportRequestResponseDto> GetGuestRequestsAsync(Guid userId, int page, int pageSize);

        /// <summary>Admin: view own requests (paged)</summary>
        Task<PagedSupportRequestResponseDto> GetAdminRequestsAsync(Guid adminUserId, int page, int pageSize);

        /// <summary>SuperAdmin: view all requests with optional filters (paged)</summary>
        Task<PagedSupportRequestResponseDto> GetAllRequestsAsync(string? status, string? role, string? search, int page, int pageSize);

        /// <summary>SuperAdmin: respond to a request</summary>
        Task<SupportRequestResponseDto> RespondAsync(Guid requestId, RespondSupportRequestDto dto);
    }
}
