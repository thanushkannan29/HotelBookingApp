using HotelBookingAppWebApi.Models.DTOs.Review;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto);
        Task<ReviewResponseDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto);
        Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId);
        Task<PagedReviewResponseDto> GetReviewsByHotelAsync(Guid hotelId, int page, int pageSize);
        Task<IEnumerable<MyReviewsResponseDto>> GetMyReviewsAsync(Guid userId);
    }
}
