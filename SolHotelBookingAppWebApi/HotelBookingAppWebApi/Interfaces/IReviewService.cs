using HotelBookingAppWebApi.Models.DTOs.Review;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto);
        Task<ReviewResponseDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto);
        Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId);
        Task<PagedReviewResponseDto> GetReviewsByHotelAsync(Guid hotelId, int page, int pageSize);
        Task<PagedReviewResponseDto> GetAdminHotelReviewsAsync(Guid adminUserId, int page, int pageSize, int? minRating = null, int? maxRating = null, string? sortDir = null);
        Task<IEnumerable<MyReviewsResponseDto>> GetMyReviewsAsync(Guid userId);
        Task<PagedMyReviewsResponseDto> GetMyReviewsPagedAsync(Guid userId, int page, int pageSize);
        Task ReplyToReviewAsync(Guid adminUserId, Guid reviewId, string reply);
    }
}