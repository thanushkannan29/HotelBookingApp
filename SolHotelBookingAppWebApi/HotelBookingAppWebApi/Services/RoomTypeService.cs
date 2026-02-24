using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class RoomTypeService : IRoomTypeService
    {
        private readonly HotelBookingContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoomTypeService(HotelBookingContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // ADD ROOM TYPE 

        public async Task AddRoomTypeAsync(Guid userId, CreateRoomTypeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user?.HotelId == null)
                    throw new Exception("Unauthorized");

                var roomType = new RoomType
                {
                    RoomTypeId = Guid.NewGuid(),
                    HotelId = user.HotelId.Value,
                    Name = dto.Name,
                    Description = dto.Description,
                    MaxOccupancy = dto.MaxOccupancy,
                    Amenities = dto.Amenities,
                    IsActive = true
                };

                await _context.RoomTypes.AddAsync(roomType);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // UPDATE ROOM TYPE 

        public async Task UpdateRoomTypeAsync(Guid userId, UpdateRoomTypeDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            var roomType = await _context.RoomTypes
                .FirstOrDefaultAsync(r => r.RoomTypeId == dto.RoomTypeId
                                       && r.HotelId == user!.HotelId);

            if (roomType == null)
                throw new Exception("RoomType not found");

            roomType.Name = dto.Name;
            roomType.Description = dto.Description;
            roomType.MaxOccupancy = dto.MaxOccupancy;
            roomType.Amenities = dto.Amenities;

            await _context.SaveChangesAsync();
        }

        // DEACTIVATE

        public async Task ToggleRoomTypeStatusAsync(Guid roomTypeId, bool isActive)
        {
            // Get HotelId from JWT
            var hotelIdClaim = _httpContextAccessor.HttpContext!
                .User.FindFirst("HotelId")?.Value;

            if (string.IsNullOrEmpty(hotelIdClaim))
                throw new UnauthorizedAccessException("Invalid token");

            var hotelId = Guid.Parse(hotelIdClaim);

            // Get room type belonging to this hotel only
            var roomType = await _context.RoomTypes
                .FirstOrDefaultAsync(r => r.RoomTypeId == roomTypeId
                                       && r.HotelId == hotelId);

            if (roomType == null)
                throw new KeyNotFoundException("RoomType not found");

            // Update status
            roomType.IsActive = isActive;

            await _context.SaveChangesAsync();
        }


        // ADD RATE

        public async Task AddRateAsync(Guid userId, CreateRoomTypeRateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(r => r.RoomTypeId == dto.RoomTypeId
                                           && r.HotelId == user!.HotelId);

                if (roomType == null)
                    throw new Exception("RoomType not found");

                if (dto.StartDate > dto.EndDate)
                    throw new Exception("Invalid date range");

                var overlapping = await _context.RoomTypeRates
                    .AnyAsync(r =>
                        r.RoomTypeId == dto.RoomTypeId &&
                        dto.StartDate <= r.EndDate &&
                        dto.EndDate >= r.StartDate);

                if (overlapping)
                    throw new Exception("Rate already exists for selected date range");

                var rate = new RoomTypeRate
                {
                    RoomTypeRateId = Guid.NewGuid(),
                    RoomTypeId = dto.RoomTypeId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Rate = dto.Rate
                };

                await _context.RoomTypeRates.AddAsync(rate);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //UPDATE RATE

        public async Task UpdateRateAsync(Guid userId, UpdateRoomTypeRateDto dto)
        {
            var rate = await _context.RoomTypeRates
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomTypeRateId == dto.RoomTypeRateId);

            if (rate == null || rate.RoomType!.HotelId !=
                (await _context.Users.FindAsync(userId))!.HotelId)
                throw new Exception("Unauthorized");

            rate.StartDate = dto.StartDate;
            rate.EndDate = dto.EndDate;
            rate.Rate = dto.Rate;

            await _context.SaveChangesAsync();
        }

        // ================= GET RATE BY DATE =================

        public async Task<decimal> GetRateByDateAsync(Guid userId, GetRateByDateRequestDto dto)
        {
            var rate = await _context.RoomTypeRates
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r =>
                    r.RoomTypeId == dto.RoomTypeId &&
                    dto.Date >= r.StartDate &&
                    dto.Date <= r.EndDate);

            if (rate == null)
                throw new Exception("Rate not found for selected date");

            return rate.Rate;
        }
    }
}
