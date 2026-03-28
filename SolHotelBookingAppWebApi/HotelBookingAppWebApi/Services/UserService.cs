using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
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

        // ── GET PROFILE ───────────────────────────────────────────────────────
        public async Task<UserProfileResponseDto> GetProfileAsync(Guid userId)
        {
            var user = await _userRepo.GetQueryable()
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new NotFoundException("User not found.");

            if (user.UserDetails == null)
                throw new UserProfileException("Profile details not found.");

            var reviewCount = await _reviewRepo.GetQueryable()
                .CountAsync(r => r.UserId == userId);

            return MapToDto(user, reviewCount);
        }

        // ── UPDATE PROFILE ────────────────────────────────────────────────────
        public async Task<UserProfileResponseDto> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.GetQueryable()
                    .Include(u => u.UserDetails)
                    .FirstOrDefaultAsync(u => u.UserId == userId)
                    ?? throw new NotFoundException("User not found.");

                if (user.UserDetails == null)
                    throw new UserProfileException("Profile details not found.");

                var d = user.UserDetails;

                if (!string.IsNullOrWhiteSpace(dto.Name)) d.Name = dto.Name;
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) d.PhoneNumber = dto.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(dto.Address)) d.Address = dto.Address;
                if (!string.IsNullOrWhiteSpace(dto.State)) d.State = dto.State;
                if (!string.IsNullOrWhiteSpace(dto.City)) d.City = dto.City;
                if (!string.IsNullOrWhiteSpace(dto.Pincode)) d.Pincode = dto.Pincode;
                if (dto.ProfileImageUrl != null) d.ProfileImageUrl = dto.ProfileImageUrl;

                await _unitOfWork.CommitAsync();
                return MapToDto(user, 0);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── BOOKING HISTORY ───────────────────────────────────────────────────
        public async Task<PagedBookingHistoryDto> GetBookingHistoryAsync(Guid userId, int page, int pageSize)
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

        // ── MAPPER ────────────────────────────────────────────────────────────
        private static UserProfileResponseDto MapToDto(User user, int reviewCount = 0)
        {
            var d = user.UserDetails!;
            return new UserProfileResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = user.Role.ToString(),
                Name = d.Name,
                PhoneNumber = d.PhoneNumber,
                Address = d.Address,
                State = d.State,
                City = d.City,
                Pincode = d.Pincode,
                ProfileImageUrl = d.ProfileImageUrl,
                CreatedAt = d.CreatedAt,
                TotalReviewPoints = reviewCount * 100
            };
        }
    }
}
