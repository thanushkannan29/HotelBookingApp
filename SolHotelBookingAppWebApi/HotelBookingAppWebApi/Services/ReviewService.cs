using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages guest reviews — creation, update, deletion, and admin replies.
    /// One review per completed reservation is enforced.
    /// Reward points are configured via ReviewSettings:RewardPoints in appsettings.json.
    /// </summary>
    public class ReviewService(
        IRepository<Guid, Review> reviewRepo,
        IRepository<Guid, Hotel> hotelRepo,
        IRepository<Guid, Reservation> reservationRepo,
        IRepository<Guid, User> userRepo,
        IWalletService walletService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration) : IReviewService
    {
        private readonly decimal _reviewRewardAmount = configuration.GetValue<decimal>("ReviewSettings:RewardPoints", 10m);

        private readonly IRepository<Guid, Review> _reviewRepo = reviewRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo = hotelRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo = reservationRepo;
        private readonly IRepository<Guid, User> _userRepo = userRepo;
        private readonly IWalletService _walletService = walletService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        // ── ADD REVIEW (one review per completed reservation) ─────────────────
        public async Task<ReviewResponseDto> AddReviewAsync(Guid userId, CreateReviewDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await EnsureHotelExistsAsync(dto.HotelId);
                var reservation = await GetCompletedReservationOrThrowAsync(userId, dto);
                await EnsureNotAlreadyReviewedAsync(dto.ReservationId);

                var review = BuildReview(userId, dto);
                await _reviewRepo.AddAsync(review);
                await _walletService.CreditAsync(userId, _reviewRewardAmount, "Review contribution reward");
                await _unitOfWork.CommitAsync();

                return MapToDto(review, reservation.ReservationCode, _reviewRewardAmount);
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
                var review = await GetReviewWithDetailsAsync(reviewId);
                EnsureReviewOwnership(review, userId);
                ApplyReviewUpdates(review, dto);

                await _reviewRepo.UpdateAsync(reviewId, review);
                await _unitOfWork.CommitAsync();

                return MapToDto(review, review.Reservation?.ReservationCode ?? string.Empty, _reviewRewardAmount);
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

                EnsureReviewOwnership(review, userId);
                await _walletService.DebitAsync(review.UserId, _reviewRewardAmount,
                    "Review contribution reversed on deletion");
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
                .Include(r => r.User!)
                    .ThenInclude(u => u.UserDetails)
                .Where(r => r.HotelId == hotelId)
                .OrderByDescending(r => r.CreatedDate);

            return await BuildPagedReviewResponseAsync(query, page, pageSize);
        }

        // ── GET HOTEL REVIEWS FOR ADMIN (with optional filters) ──────────────
        public async Task<PagedReviewResponseDto> GetAdminHotelReviewsAsync(
            Guid adminUserId, int page, int pageSize,
            int? minRating = null, int? maxRating = null, string? sortDir = null)
        {
            var admin = await GetAdminWithHotelAsync(adminUserId);
            var query = BuildAdminReviewQuery(admin.HotelId!.Value, minRating, maxRating, sortDir);
            return await BuildPagedReviewResponseAsync(query, page, pageSize);
        }

        private IQueryable<Review> BuildAdminReviewQuery(
            Guid hotelId, int? minRating, int? maxRating, string? sortDir)
        {
            var query = _reviewRepo.GetQueryable()
                .Include(r => r.Reservation)
                .Include(r => r.User!).ThenInclude(u => u.UserDetails)
                .Where(r => r.HotelId == hotelId)
                .AsQueryable();

            if (minRating.HasValue) query = query.Where(r => r.Rating >= minRating.Value);
            if (maxRating.HasValue) query = query.Where(r => r.Rating <= maxRating.Value);

            return sortDir?.ToLower() switch
            {
                "asc"  => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedDate),
                "desc" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedDate),
                _      => query.OrderByDescending(r => r.CreatedDate)
            };
        }

        public async Task<IEnumerable<MyReviewsResponseDto>> GetMyReviewsAsync(Guid userId)
        {
            var reviews = await _reviewRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Include(r => r.Reservation)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return reviews.Select(r => MapToMyDto(r, _reviewRewardAmount));
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

            return new PagedMyReviewsResponseDto
            {
                TotalCount = total,
                Reviews = reviews.Select(r => MapToMyDto(r, _reviewRewardAmount))
            };
        }

        // ── ADMIN: REPLY TO REVIEW ────────────────────────────────────────────
        public async Task ReplyToReviewAsync(Guid adminUserId, Guid reviewId, string reply)
        {
            var admin = await GetAdminWithHotelAsync(adminUserId);

            var review = await _reviewRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.HotelId == admin.HotelId)
                ?? throw new NotFoundException("Review not found or does not belong to your hotel.");

            review.AdminReply = reply;
            await _unitOfWork.SaveChangesAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task EnsureHotelExistsAsync(Guid hotelId)
        {
            var exists = await _hotelRepo.GetQueryable().AnyAsync(h => h.HotelId == hotelId);
            if (!exists) throw new NotFoundException("Hotel not found.");
        }

        private async Task<Reservation> GetCompletedReservationOrThrowAsync(Guid userId, CreateReviewDto dto)
        {
            return await _reservationRepo.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.ReservationId == dto.ReservationId &&
                    r.UserId == userId &&
                    r.HotelId == dto.HotelId &&
                    r.Status == ReservationStatus.Completed)
                ?? throw new ReviewException(
                    "You can only review a completed reservation. Verify the reservation belongs to you and is completed.");
        }

        private async Task EnsureNotAlreadyReviewedAsync(Guid reservationId)
        {
            var exists = await _reviewRepo.GetQueryable()
                .AnyAsync(r => r.ReservationId == reservationId);
            if (exists) throw new ReviewException("You have already submitted a review for this reservation.");
        }

        private async Task<Review> GetReviewWithDetailsAsync(Guid reviewId)
            => await _reviewRepo.GetQueryable()
                .Include(r => r.Reservation)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId)
                ?? throw new NotFoundException("Review not found.");

        private async Task<User> GetAdminWithHotelAsync(Guid adminUserId)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.HotelId is null)
                throw new UnAuthorizedException("No hotel associated with this admin.");
            return admin;
        }

        private async Task<PagedReviewResponseDto> BuildPagedReviewResponseAsync(
            IQueryable<Review> query, int page, int pageSize)
        {
            var total = await query.CountAsync();
            var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedReviewResponseDto
            {
                TotalCount = total,
                Reviews = reviews.Select(r => MapToDto(r, r.Reservation?.ReservationCode ?? string.Empty, _reviewRewardAmount))
            };
        }

        private static void EnsureReviewOwnership(Review review, Guid userId)
        {
            if (review.UserId != userId)
                throw new ReviewException("You are not allowed to modify this review.");
        }

        private static void ApplyReviewUpdates(Review review, UpdateReviewDto dto)
        {
            review.Rating = dto.Rating;
            if (!string.IsNullOrWhiteSpace(dto.Comment)) review.Comment = dto.Comment;
            if (dto.ImageUrl is not null) review.ImageUrl = dto.ImageUrl;
        }

        private static Review BuildReview(Guid userId, CreateReviewDto dto) => new()
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

        private static ReviewResponseDto MapToDto(Review review, string reservationCode, decimal rewardAmount) => new()
        {
            ReviewId = review.ReviewId,
            HotelId = review.HotelId,
            UserId = review.UserId,
            UserName = review.User?.Name ?? string.Empty,
            UserProfileImageUrl = review.User?.UserDetails?.ProfileImageUrl,
            ReservationId = review.ReservationId,
            ReservationCode = reservationCode,
            Rating = review.Rating,
            Comment = review.Comment,
            ImageUrl = review.ImageUrl,
            AdminReply = review.AdminReply,
            CreatedDate = review.CreatedDate,
            ContributionPoints = (int)rewardAmount
        };

        private static MyReviewsResponseDto MapToMyDto(Review review, decimal rewardAmount) => new()
        {
            ReviewId = review.ReviewId,
            HotelId = review.HotelId,
            HotelName = review.Hotel?.Name ?? string.Empty,
            ReservationId = review.ReservationId,
            ReservationCode = review.Reservation?.ReservationCode ?? string.Empty,
            Rating = review.Rating,
            Comment = review.Comment,
            ImageUrl = review.ImageUrl,
            CreatedDate = review.CreatedDate,
            ContributionPoints = (int)rewardAmount
        };
    }
}
