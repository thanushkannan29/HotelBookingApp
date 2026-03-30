using HotelBookingAppWebApi.Models.DTOs.Review;

namespace HotelBookingAppWebApi.Interfaces
{
    /// <summary>Guest review lifecycle and admin reply management.</summary>
    public interface IReviewService
    {
        /// <summary>Adds a review for a completed reservation and credits the guest ₹100 reward.</summary>
        Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto);

        /// <summary>Updates rating, comment, or image for an existing review owned by the guest.</summary>
        Task<ReviewResponseDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto);

        /// <summary>Deletes a review and reverses the ₹100 wallet reward.</summary>
        Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId);

        /// <summary>Returns paged public reviews for a hotel.</summary>
        Task<PagedReviewResponseDto> GetReviewsByHotelAsync(Guid hotelId, int page, int pageSize);

        /// <summary>Admin view of reviews for their hotel with optional rating filter and sort.</summary>
        Task<PagedReviewResponseDto> GetAdminHotelReviewsAsync(
            Guid adminUserId, int page, int pageSize,
            int? minRating = null, int? maxRating = null, string? sortDir = null);

        /// <summary>Returns paged reviews submitted by the authenticated guest.</summary>
        Task<PagedMyReviewsResponseDto> GetMyReviewsPagedAsync(Guid userId, int page, int pageSize);

        /// <summary>Admin posts a reply to a guest review on their hotel.</summary>
        Task ReplyToReviewAsync(Guid adminUserId, Guid reviewId, string reply);
    }
}
