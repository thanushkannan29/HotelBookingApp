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
<<<<<<< Updated upstream
    public class ReviewService : IReviewService
    {
        private readonly IRepository<Guid, Review> _reviewRepo;
        private readonly IRepository<Guid, Hotel> _hotelRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IWalletService _walletService;
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(
            IRepository<Guid, Review> reviewRepo,
            IRepository<Guid, Hotel> hotelRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, User> userRepo,
            IWalletService walletService,
            IUnitOfWork unitOfWork)
        {
            _reviewRepo = reviewRepo;
            _hotelRepo = hotelRepo;
            _reservationRepo = reservationRepo;
            _userRepo = userRepo;
            _walletService = walletService;
            _unitOfWork = unitOfWork;
        }
=======
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
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
                await _walletService.CreditAsync(userId, 100m, "Review contribution reward");
=======
                await _walletService.CreditAsync(userId, _reviewRewardAmount, "Review contribution reward");
>>>>>>> Stashed changes
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
                var review = await _reviewRepo.GetQueryable()
                    .Include(r => r.Reservation)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId)
                    ?? throw new NotFoundException("Review not found.");

                if (review.UserId != userId)
                    throw new ReviewException("You are not allowed to update this review.");

                review.Rating = dto.Rating;
                if (!string.IsNullOrWhiteSpace(dto.Comment)) review.Comment = dto.Comment;
                if (dto.ImageUrl != null) review.ImageUrl = dto.ImageUrl;

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

<<<<<<< Updated upstream
                if (review.UserId != userId)
                    throw new ReviewException("You are not allowed to delete this review.");

                await _walletService.DebitAsync(review.UserId, 100m, "Review contribution reversed on deletion");
=======
                await _walletService.DebitAsync(review.UserId, _reviewRewardAmount,
                    "Review contribution reversed on deletion");
>>>>>>> Stashed changes
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

            var total = await query.CountAsync();
            var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedReviewResponseDto
            {
                TotalCount = total,
                Reviews = reviews.Select(r => MapToDto(r, r.Reservation?.ReservationCode ?? string.Empty))
            };
        }

        // ── GET HOTEL REVIEWS FOR ADMIN (looks up hotel from admin's userId) ──
        public async Task<PagedReviewResponseDto> GetAdminHotelReviewsAsync(Guid adminUserId, int page, int pageSize)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.HotelId == null)
                throw new UnAuthorizedException("No hotel associated with this admin.");
            return await GetReviewsByHotelAsync(admin.HotelId.Value, page, pageSize);
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

<<<<<<< Updated upstream
            return new PagedMyReviewsResponseDto { TotalCount = total, Reviews = reviews.Select(MapToMyDto) };
=======
            return new PagedMyReviewsResponseDto
            {
                TotalCount = total,
                Reviews = reviews.Select(r => MapToMyDto(r, _reviewRewardAmount))
            };
>>>>>>> Stashed changes
        }

        // ── ADMIN: REPLY TO REVIEW ────────────────────────────────────────────
        public async Task ReplyToReviewAsync(Guid adminUserId, Guid reviewId, string reply)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.HotelId == null)
                throw new UnAuthorizedException("No hotel associated with this admin.");

            var review = await _reviewRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.HotelId == admin.HotelId)
                ?? throw new NotFoundException("Review not found or does not belong to your hotel.");

            review.AdminReply = reply;
            await _unitOfWork.SaveChangesAsync();
        }

        private static ReviewResponseDto MapToDto(Review r, string reservationCode) => new()
        {
<<<<<<< Updated upstream
            ReviewId = r.ReviewId,
            HotelId = r.HotelId,
            UserId = r.UserId,
            UserName = r.User?.Name ?? string.Empty,
            UserProfileImageUrl = r.User?.UserDetails?.ProfileImageUrl,
            ReservationId = r.ReservationId,
            ReservationCode = reservationCode,
            Rating = r.Rating,
            Comment = r.Comment,
            ImageUrl = r.ImageUrl,
            AdminReply = r.AdminReply,
            CreatedDate = r.CreatedDate,
            ContributionPoints = 100
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
            CreatedDate = r.CreatedDate,
            ContributionPoints = 100
=======
            var exists = await _hotelRepo.GetQueryable().AnyAsync(h => h.HotelId == hotelId);
            if (!exists) throw new NotFoundException("Hotel not found.");
        }

        private async Task<Reservation> GetCompletedReservationOrThrowAsync(
            Guid userId, CreateReviewDto dto)
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
>>>>>>> Stashed changes
        };
    }
}