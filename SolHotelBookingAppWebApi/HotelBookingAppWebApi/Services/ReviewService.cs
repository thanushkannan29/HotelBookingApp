using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.Repository;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Repository;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;

        public ReviewService(
            IReviewRepository reviewRepo,
            IRepository<Guid, Hotel> hotelRepo)
        {
            _reviewRepo = reviewRepo;
            _hotelRepo = hotelRepo;
        }

        // ADD REVIEW

        public async Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto)
        {
            var hotelExists = await _hotelRepo.ExistsAsync(dto.HotelId);

            if (!hotelExists)
                throw new NotFoundException("Hotel not found.");

            var reviews = await _reviewRepo.FindAsync(r =>
                r.HotelId == dto.HotelId && r.UserId == userId);

            if (reviews.Any())
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

        // UPDATE REVIEW

        public async Task<ReviewResponseDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);

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

        // DELETE REVIEW

        public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
        {
            var review = await _reviewRepo.GetByIdAsync(reviewId);

            if (review == null)
                throw new NotFoundException("Review not found.");

            if (review.UserId != userId)
                throw new ReviewException("You can delete only your own review.");

            return await _reviewRepo.DeleteAsync(reviewId);
        }

        // GET REVIEWS BY HOTEL

        public async Task<PagedReviewResponseDto> GetReviewsByHotelAsync(Guid hotelId, int page, int pageSize)
        {
            var reviews = await _reviewRepo.GetReviewsByHotelAsync(hotelId);

            var total = reviews.Count();

            var paged = reviews
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return new PagedReviewResponseDto
            {
                TotalCount = total,
                Reviews = paged
            };
        }

        // GET MY REVIEWS

        public async Task<IEnumerable<MyReviewsResponseDto>> GetMyReviewsAsync(Guid userId)
        {
            var reviews = await _reviewRepo.GetReviewsByUserAsync(userId);

            return reviews.Select(r => new MyReviewsResponseDto
            {
                ReviewId = r.ReviewId,
                HotelId = r.HotelId,
                HotelName = r.Hotel?.Name ?? "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedDate = r.CreatedDate
            });
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
