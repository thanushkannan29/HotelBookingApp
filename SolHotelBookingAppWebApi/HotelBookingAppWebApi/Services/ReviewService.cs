using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IRepository<Guid, Review> _reviewRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(
            IRepository<Guid, Review> reviewRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IUnitOfWork unitOfWork)
        {
            _reviewRepo = reviewRepo;
            _hotelRepo = hotelRepo;
            _unitOfWork = unitOfWork;
        }

        #region ADD

        public async Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                //  Validate hotel
                var hotelExists = await _hotelRepo.GetQueryable()
                    .AnyAsync(h => h.HotelId == dto.HotelId);

                if (!hotelExists)
                    throw new NotFoundException("Hotel not found");

                //  Prevent duplicate review
                var alreadyReviewed = await _reviewRepo.GetQueryable()
                    .AnyAsync(r => r.HotelId == dto.HotelId && r.UserId == userId);

                if (alreadyReviewed)
                    throw new ReviewException("Already reviewed");

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

                await _unitOfWork.CommitAsync();

                return MapToDto(review);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region UPDATE

        public async Task<ReviewResponseDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var review = await _reviewRepo.GetAsync(reviewId)
                    ?? throw new NotFoundException("Review not found");

                if (review.UserId != userId)
                    throw new ReviewException("Not allowed");

                review.Rating = dto.Rating;

                if (!string.IsNullOrWhiteSpace(dto.Comment))
                    review.Comment = dto.Comment;

                await _reviewRepo.UpdateAsync(reviewId, review);

                await _unitOfWork.CommitAsync();

                return MapToDto(review);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region DELETE

        public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var review = await _reviewRepo.GetAsync(reviewId)
                    ?? throw new NotFoundException("Review not found");

                if (review.UserId != userId)
                    throw new ReviewException("Not allowed");

                var deleted = await _reviewRepo.DeleteAsync(reviewId);

                await _unitOfWork.CommitAsync();

                return deleted != null;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region GET

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

        #endregion

        #region HELPER

        private static ReviewResponseDto MapToDto(Review r) => new()
        {
            ReviewId = r.ReviewId,
            HotelId = r.HotelId,
            UserId = r.UserId,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedDate = r.CreatedDate
        };

        #endregion
    }
}
