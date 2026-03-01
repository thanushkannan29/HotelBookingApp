using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.EntityFrameworkCore;
namespace HotelBookingAppWebApi.Services
{
    public class UserService : IUserService
    {
        private readonly HotelBookingContext _context;

        public UserService(HotelBookingContext context)
        {
            _context = context;
        }

        // ============================================
        // GET PROFILE
        // ============================================
        public async Task<UserProfileResponseDto> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new NotFoundException("User not found.");

            if (user.UserDetails == null)
                throw new UserProfileException("Profile details not found.");

            return MapToDto(user);
        }

        // ============================================
        // UPDATE PROFILE (Transactional)
        // ============================================
        public async Task<UserProfileResponseDto> UpdateProfileAsync(
            Guid userId,
            UpdateUserProfileDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var user = await _context.Users
                .Include(u => u.UserDetails)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new NotFoundException("User not found.");

            if (user.UserDetails == null)
                throw new UserProfileException("Profile details not found.");

            var details = user.UserDetails;

            if (!string.IsNullOrWhiteSpace(dto.Name))
                details.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                details.PhoneNumber = dto.PhoneNumber;

            if (!string.IsNullOrWhiteSpace(dto.Address))
                details.Address = dto.Address;

            if (!string.IsNullOrWhiteSpace(dto.State))
                details.State = dto.State;

            if (!string.IsNullOrWhiteSpace(dto.City))
                details.City = dto.City;

            if (!string.IsNullOrWhiteSpace(dto.Pincode))
                details.Pincode = dto.Pincode;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToDto(user);
        }

        // ============================================
        // BOOKING HISTORY (PAGINATED)
        // ============================================
        public async Task<PagedBookingHistoryDto> GetBookingHistoryAsync(
            Guid userId,
            int page,
            int pageSize)
        {
            var query = _context.Reservations
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
                    Status = r.Status,
                    CreatedDate = r.CreatedDate
                })
                .ToListAsync();

            return new PagedBookingHistoryDto
            {
                TotalCount = total,
                Bookings = bookings
            };
        }

        private static UserProfileResponseDto MapToDto(User user)
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
                CreatedAt = d.CreatedAt
            };
        }
    }
}
