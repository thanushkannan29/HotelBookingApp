using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Microsoft.EntityFrameworkCore;
namespace HotelBookingAppWebApi.Services
{   
    public class RoomService : IRoomService
    {
        private readonly HotelBookingContext _context;

        public RoomService(HotelBookingContext context)
        {
            _context = context;
        }

        // ADD ROOM 

        public async Task AddRoomAsync(Guid userId, CreateRoomDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user?.HotelId == null)
                    throw new Exception("Unauthorized");

                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt => rt.RoomTypeId == dto.RoomTypeId
                                            && rt.HotelId == user.HotelId);

                if (roomType == null)
                    throw new Exception("Invalid RoomType");

                var existing = await _context.Rooms
                    .AnyAsync(r => r.HotelId == user.HotelId
                                && r.RoomNumber == dto.RoomNumber);

                if (existing)
                    throw new Exception("Room number already exists");

                var room = new Room
                {
                    RoomId = Guid.NewGuid(),
                    RoomNumber = dto.RoomNumber,
                    Floor = dto.Floor,
                    HotelId = user.HotelId.Value,
                    RoomTypeId = dto.RoomTypeId,
                    IsActive = true
                };

                await _context.Rooms.AddAsync(room);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // UPDATE ROOM

        public async Task UpdateRoomAsync(Guid userId, UpdateRoomDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FindAsync(userId);

                var room = await _context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId
                                           && r.HotelId == user!.HotelId);

                if (room == null)
                    throw new Exception("Room not found");

                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt => rt.RoomTypeId == dto.RoomTypeId
                                            && rt.HotelId == user.HotelId);

                if (roomType == null)
                    throw new Exception("Invalid RoomType");

                room.RoomNumber = dto.RoomNumber;
                room.Floor = dto.Floor;
                room.RoomTypeId = dto.RoomTypeId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // TOGGLE ROOM STATUS

        public async Task ToggleRoomStatusAsync(Guid userId, Guid roomId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId
                                       && r.HotelId == user!.HotelId);

            if (room == null)
                throw new Exception("Room not found");

            room.IsActive = isActive;

            await _context.SaveChangesAsync();
        }

        // LIST ROOMS 

        public async Task<IEnumerable<RoomListResponseDto>> GetRoomsByHotelAsync(
    Guid userId, int pageNumber, int pageSize)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user?.HotelId == null)
                throw new Exception("Unauthorized");

            var offset = (pageNumber - 1) * pageSize;

            var result = await _context.RoomListQueryModel
                .FromSqlRaw("EXEC proc_GetRoomsByHotel {0},{1},{2}",
                    user.HotelId, offset, pageSize)
                .ToListAsync();

            return result.Select(r => new RoomListResponseDto
            {
                RoomId = r.RoomId,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomTypeName,
                IsActive = r.IsActive
            });
        }

    }
}
