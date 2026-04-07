using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IAmenityRequestService
    {
        Task<AmenityRequestResponseDto> CreateRequestAsync(Guid adminUserId, CreateAmenityRequestDto dto);
        Task<IEnumerable<AmenityRequestResponseDto>> GetAdminRequestsAsync(Guid adminUserId);
        Task<PagedAmenityRequestResponseDto> GetAdminRequestsPagedAsync(Guid adminUserId, int page, int pageSize, string? search = null);
        Task<PagedAmenityRequestResponseDto> GetAllRequestsAsync(string? status, int page, int pageSize);
        Task<AmenityRequestResponseDto> ApproveRequestAsync(Guid requestId, Guid superAdminUserId);
        Task<AmenityRequestResponseDto> RejectRequestAsync(Guid requestId, Guid superAdminUserId, string note);
    }
}
