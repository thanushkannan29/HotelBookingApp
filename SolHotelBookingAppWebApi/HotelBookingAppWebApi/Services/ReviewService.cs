using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IRepository<Guid, Review> _reviewRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;

        public ReviewService(
            IRepository<Guid, Review> reviewRepo,
            IRepository<Guid, Hotel> hotelRepo)
        {
            _reviewRepo = reviewRepo;
            _hotelRepo = hotelRepo;
        }

        //  ADD REVIEW
        public async Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto)
        {
            // Check hotel exists
            var hotelExists = await _hotelRepo.GetQueryable()
                .AnyAsync(h => h.HotelId == dto.HotelId);

            if (!hotelExists)
                throw new NotFoundException("Hotel not found.");

            // Check already reviewed
            var alreadyReviewed = await _reviewRepo.GetQueryable()
                .AnyAsync(r => r.HotelId == dto.HotelId && r.UserId == userId);

            if (alreadyReviewed)
                throw new ReviewException("You already reviewed this hotel.");

            var review = new Review
            {
                ReviewId = Guid.NewGuid(),
                HotelId = dto.HotelId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedDate = DateTime.UtcNow
            };

            await _reviewRepo.AddAsync(review);

            return MapToDto(review);
        }

        //  UPDATE REVIEW
        public async Task<ReviewResponseDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto)
        {
            var review = await _reviewRepo.GetAsync(reviewId);

            if (review == null)
                throw new NotFoundException("Review not found.");

            if (review.UserId != userId)
                throw new ReviewException("You can update only your own review.");

            review.Rating = dto.Rating;

            if (!string.IsNullOrWhiteSpace(dto.Comment))
                review.Comment = dto.Comment;

            await _reviewRepo.UpdateAsync(reviewId, review);

            return MapToDto(review);
        }

        //  DELETE REVIEW
        public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
        {
            var review = await _reviewRepo.GetAsync(reviewId);

            if (review == null)
                throw new NotFoundException("Review not found.");

            if (review.UserId != userId)
                throw new ReviewException("You can delete only your own review.");

            var deleted = await _reviewRepo.DeleteAsync(reviewId);

            return deleted != null;
        }

        //  GET REVIEWS BY HOTEL (WITH PAGINATION)
        public async Task<PagedReviewResponseDto> GetReviewsByHotelAsync(Guid hotelId, int page, int pageSize)
        {
            var query = _reviewRepo.GetQueryable()
                .Where(r => r.HotelId == hotelId)
                .OrderByDescending(r => r.CreatedDate);

            var total = await query.CountAsync();

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedReviewResponseDto
            {
                TotalCount = total,
                Reviews = reviews.Select(MapToDto)
            };
        }

        //  GET MY REVIEWS (WITH HOTEL NAME)
        public async Task<IEnumerable<MyReviewsResponseDto>> GetMyReviewsAsync(Guid userId)
        {
            var reviews = await _reviewRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(r => new MyReviewsResponseDto
            {
                ReviewId = r.ReviewId,
                HotelId = r.HotelId,
                HotelName = r.Hotel!.Name,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedDate = r.CreatedDate
            });
        }

        //  MAPPER
        private static ReviewResponseDto MapToDto(Review r)
        {
            return new ReviewResponseDto
            {
                ReviewId = r.ReviewId,
                HotelId = r.HotelId,
                UserId = r.UserId,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedDate = r.CreatedDate
            };
        }
    }
}
