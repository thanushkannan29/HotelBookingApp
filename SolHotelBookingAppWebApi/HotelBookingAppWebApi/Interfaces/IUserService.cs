using HotelBookingAppWebApi.Models.DTOs.UserDetails;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponseDto> GetProfileAsync(Guid userId);
        Task<UserProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto);
        Task<PagedBookingHistoryDto> GetBookingHistoryAsync(Guid userId, int page, int pageSize);
    }
}
