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
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(
            IRepository<Guid, Review> reviewRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IUnitOfWork unitOfWork)
        {
            _reviewRepo = reviewRepo;
            _hotelRepo = hotelRepo;
            _reservationRepo = reservationRepo;
            _unitOfWork = unitOfWork;
        }

        // ── ADD REVIEW (one review per completed reservation) ─────────────────
        public async Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var hotelExists = await _hotelRepo.GetQueryable()
                    .AnyAsync(h => h.HotelId == dto.HotelId);

                if (!hotelExists)
                    throw new NotFoundException("Hotel not found.");

                // Verify the reservation exists, belongs to this user, belongs to this hotel, and is Completed
                var reservation = await _reservationRepo.GetQueryable()
                    .FirstOrDefaultAsync(r =>
                        r.ReservationId == dto.ReservationId &&
                        r.UserId == userId &&
                        r.HotelId == dto.HotelId &&
                        r.Status == ReservationStatus.Completed);

                if (reservation == null)
                    throw new ReviewException(
                        "You can only review a completed reservation. Verify the reservation belongs to you and is completed.");

                // One review per reservation
                var alreadyReviewed = await _reviewRepo.GetQueryable()
                    .AnyAsync(r => r.ReservationId == dto.ReservationId);

                if (alreadyReviewed)
                    throw new ReviewException("You have already submitted a review for this reservation.");

                var review = new Review
                {
                    ReviewId = Guid.NewGuid(),
                    HotelId = dto.HotelId,
                    UserId = userId,
                    ReservationId = dto.ReservationId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    ImageUrl = dto.ImageUrl,
                    CreatedDate = DateTime.UtcNow
                };

                await _reviewRepo.AddAsync(review);
                await _unitOfWork.CommitAsync();

                return MapToDto(review, reservation.ReservationCode);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── UPDATE REVIEW ─────────────────────────────────────────────────────
        public async Task<ReviewResponseDto> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var review = await _reviewRepo.GetQueryable()
                    .Include(r => r.Reservation)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId)
                    ?? throw new NotFoundException("Review not found.");

                if (review.UserId != userId)
                    throw new ReviewException("You are not allowed to update this review.");

                review.Rating = dto.Rating;
                if (!string.IsNullOrWhiteSpace(dto.Comment)) review.Comment = dto.Comment;
                if (dto.ImageUrl != null) review.ImageUrl = dto.ImageUrl;

                await _reviewRepo.UpdateAsync(reviewId, review);
                await _unitOfWork.CommitAsync();

                return MapToDto(review, review.Reservation?.ReservationCode ?? string.Empty);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── DELETE REVIEW ─────────────────────────────────────────────────────
        public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var review = await _reviewRepo.GetAsync(reviewId)
                    ?? throw new NotFoundException("Review not found.");

                if (review.UserId != userId)
                    throw new ReviewException("You are not allowed to delete this review.");

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

        // ── GET REVIEWS BY HOTEL ──────────────────────────────────────────────
        public async Task<PagedReviewResponseDto> GetReviewsByHotelAsync(Guid hotelId, int page, int pageSize)
        {
            var query = _reviewRepo.GetQueryable()
                .Include(r => r.Reservation)
                .Where(r => r.HotelId == hotelId)
                .OrderByDescending(r => r.CreatedDate);

            var total = await query.CountAsync();
            var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedReviewResponseDto
            {
                TotalCount = total,
                Reviews = reviews.Select(r => MapToDto(r, r.Reservation?.ReservationCode ?? string.Empty))
            };
        }

        // ── GET MY REVIEWS (non-paged) ────────────────────────────────────────
        public async Task<IEnumerable<MyReviewsResponseDto>> GetMyReviewsAsync(Guid userId)
        {
            var reviews = await _reviewRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Include(r => r.Reservation)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(MapToMyDto);
        }

        // ── GET MY REVIEWS (paged) ────────────────────────────────────────────
        public async Task<PagedMyReviewsResponseDto> GetMyReviewsPagedAsync(Guid userId, int page, int pageSize)
        {
            var query = _reviewRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Include(r => r.Reservation)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate);

            var total = await query.CountAsync();
            var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedMyReviewsResponseDto { TotalCount = total, Reviews = reviews.Select(MapToMyDto) };
        }

        private static ReviewResponseDto MapToDto(Review r, string reservationCode) => new()
        {
            ReviewId = r.ReviewId,
            HotelId = r.HotelId,
            UserId = r.UserId,
            ReservationId = r.ReservationId,
            ReservationCode = reservationCode,
            Rating = r.Rating,
            Comment = r.Comment,
            ImageUrl = r.ImageUrl,
            CreatedDate = r.CreatedDate
        };

        private static MyReviewsResponseDto MapToMyDto(Review r) => new()
        {
            ReviewId = r.ReviewId,
            HotelId = r.HotelId,
            HotelName = r.Hotel?.Name ?? string.Empty,
            ReservationId = r.ReservationId,
            ReservationCode = r.Reservation?.ReservationCode ?? string.Empty,
            Rating = r.Rating,
            Comment = r.Comment,
            ImageUrl = r.ImageUrl,
            CreatedDate = r.CreatedDate
        };
    }
}