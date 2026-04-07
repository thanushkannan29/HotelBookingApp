using HotelBookingAppWebApi.Models.DTOs.UserDetails;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>User profile management and booking history for authenticated users.</summary>
    public interface IUserService
    {
        /// <summary>Returns the user's profile details. Auto-creates missing profile records for seeded accounts.</summary>
        Task<UserProfileResponseDto> GetProfileAsync(Guid userId);

        /// <summary>Updates profile fields. Only non-null/non-empty values are applied (partial update).</summary>
        Task<UserProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto);

        /// <summary>Returns paged booking history for the authenticated guest.</summary>
        Task<PagedBookingHistoryDto> GetBookingHistoryAsync(Guid userId, int page, int pageSize);
    }
}
