using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class ReviewService : IReviewService
    {
        private readonly HotelBookingContext _context;

        public ReviewService(HotelBookingContext context)
        {
            _context = context;
        }

        // ============================================
        // ADD REVIEW
        // ============================================
        public async Task<ReviewResponseDto> AddReviewAsync(
            Guid userId,
            CreateReviewDto dto)
        {
            var hotelExists = await _context.Hotels
                .AnyAsync(h => h.HotelId == dto.HotelId);

            if (!hotelExists)
                throw new NotFoundException("Hotel not found.");

            var alreadyReviewed = await _context.Reviews
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

            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();

            return MapToDto(review);
        }

        // ============================================
        // UPDATE REVIEW
        // ============================================
        public async Task<ReviewResponseDto> UpdateReviewAsync(
            Guid userId,
            Guid reviewId,
            UpdateReviewDto dto)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                throw new NotFoundException("Review not found.");

            if (review.UserId != userId)
                throw new ReviewException("You can update only your own review.");

            review.Rating = dto.Rating;
            if (!string.IsNullOrWhiteSpace(dto.Comment))
                review.Comment = dto.Comment;

            await _context.SaveChangesAsync();

            return MapToDto(review);
        }

        // ============================================
        // DELETE REVIEW
        // ============================================
        public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null)
                throw new NotFoundException("Review not found.");

            if (review.UserId != userId)
                throw new ReviewException("You can delete only your own review.");

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return true;
        }

        // ============================================
        // GET REVIEWS BY HOTEL (PAGINATED)
        // ============================================
        public async Task<PagedReviewResponseDto> GetReviewsByHotelAsync(
            Guid hotelId,
            int page,
            int pageSize)
        {
            var query = _context.Reviews
                .Where(r => r.HotelId == hotelId)
                .OrderByDescending(r => r.CreatedDate);

            var total = await query.CountAsync();

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => MapToDto(r))
                .ToListAsync();

            return new PagedReviewResponseDto
            {
                TotalCount = total,
                Reviews = reviews
            };
        }

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
