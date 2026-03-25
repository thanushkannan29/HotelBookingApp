using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IAmenityRequestService
    {
        Task<AmenityRequestResponseDto> CreateRequestAsync(Guid adminUserId, CreateAmenityRequestDto dto);
        Task<IEnumerable<AmenityRequestResponseDto>> GetAdminRequestsAsync(Guid adminUserId);
        Task<PagedAmenityRequestResponseDto> GetAllRequestsAsync(string? status, int page, int pageSize);
        Task<AmenityRequestResponseDto> ApproveRequestAsync(Guid requestId, Guid superAdminUserId);
        Task<AmenityRequestResponseDto> RejectRequestAsync(Guid requestId, Guid superAdminUserId, string note);
    }
}
