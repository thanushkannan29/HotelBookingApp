using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages user profile retrieval, updates, and booking history.
    /// Auto-creates UserProfileDetails for accounts that were seeded without one.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IRepository<Guid, Reservation> _reservationRepo;
        private readonly IRepository<Guid, Review> _reviewRepo;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            IRepository<Guid, User> userRepo,
            IRepository<Guid, Reservation> reservationRepo,
            IRepository<Guid, Review> reviewRepo,
            IUnitOfWork unitOfWork)
        {
            _userRepo = userRepo;
            _reservationRepo = reservationRepo;
            _reviewRepo = reviewRepo;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task<UserProfileResponseDto> GetProfileAsync(Guid userId)
        {
            var user = await GetUserWithDetailsOrThrowAsync(userId);
            await EnsureProfileDetailsExistAsync(user);

            var reviewCount = await _reviewRepo.GetQueryable()
                .CountAsync(r => r.UserId == userId);

            return MapToDto(user, reviewCount);
        }

        public async Task<UserProfileResponseDto> UpdateProfileAsync(
            Guid userId, UpdateUserProfileDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await GetUserWithDetailsOrThrowAsync(userId);
                if (user.UserDetails is null)
                    throw new UserProfileException("Profile details not found.");

                ApplyProfileUpdates(user, dto);
                await _unitOfWork.CommitAsync();
                return MapToDto(user, 0);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<PagedBookingHistoryDto> GetBookingHistoryAsync(
            Guid userId, int page, int pageSize)
        {
            var query = _reservationRepo.GetQueryable()
                .Include(r => r.Hotel)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedDate);

            var total = await query.CountAsync();
            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new BookingHistoryDto
                {
                    ReservationId = r.ReservationId,
                    ReservationCode = r.ReservationCode,
                    HotelName = r.Hotel!.Name,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    TotalAmount = r.TotalAmount,
                    Status = r.Status.ToString(),
                    CreatedDate = r.CreatedDate
                })
                .ToListAsync();

            return new PagedBookingHistoryDto { TotalCount = total, Bookings = bookings };
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<User> GetUserWithDetailsOrThrowAsync(Guid userId)
            => await _userRepo.GetQueryable()
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new NotFoundException("User not found.");

        private async Task EnsureProfileDetailsExistAsync(User user)
        {
            if (user.UserDetails is not null) return;

            user.UserDetails = new UserProfileDetails
            {
                UserDetailsId = Guid.NewGuid(),
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                PhoneNumber = string.Empty,
                Address = string.Empty,
                State = string.Empty,
                City = string.Empty,
                Pincode = string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SaveChangesAsync();
        }

        private static void ApplyProfileUpdates(User user, UpdateUserProfileDto dto)
        {
            var details = user.UserDetails!;
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                details.Name = dto.Name;
                user.Name = dto.Name; // keep User.Name in sync so reviews show updated name
            }
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) details.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(dto.Address)) details.Address = dto.Address;
            if (!string.IsNullOrWhiteSpace(dto.State)) details.State = dto.State;
            if (!string.IsNullOrWhiteSpace(dto.City)) details.City = dto.City;
            if (!string.IsNullOrWhiteSpace(dto.Pincode)) details.Pincode = dto.Pincode;
            if (dto.ProfileImageUrl is not null) details.ProfileImageUrl = dto.ProfileImageUrl;
        }

        private static UserProfileResponseDto MapToDto(User user, int reviewCount)
        {
            var details = user.UserDetails!;
            return new UserProfileResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role.ToString(),
                Name = details.Name,
                PhoneNumber = details.PhoneNumber,
                Address = details.Address,
                State = details.State,
                City = details.City,
                Pincode = details.Pincode,
                ProfileImageUrl = details.ProfileImageUrl,
                CreatedAt = details.CreatedAt,
                TotalReviewPoints = reviewCount * 10
            };
        }
    }
}
