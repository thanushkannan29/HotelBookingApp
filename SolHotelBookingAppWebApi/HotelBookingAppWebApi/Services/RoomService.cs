using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRepository<Guid, Room> _roomRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;

        public RoomService(
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, User> userRepo,
            IUnitOfWork unitOfWork)
        {
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _inventoryRepo = inventoryRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        #region ADD

        public async Task AddRoomAsync(Guid userId, CreateRoomDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized");

                if (user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized");

                // Validate RoomType
                var roomTypeExists = await _roomTypeRepo.GetQueryable()
                    .AnyAsync(rt => rt.RoomTypeId == dto.RoomTypeId &&
                                    rt.HotelId == user.HotelId);

                if (!roomTypeExists)
                    throw new NotFoundException("Invalid RoomType");

                // Unique Room Number
                var exists = await _roomRepo.GetQueryable()
                    .AnyAsync(r => r.HotelId == user.HotelId &&
                                   r.RoomNumber == dto.RoomNumber);

                if (exists)
                    throw new ConflictException("Room number already exists");

                // Inventory Check
                var currentCount = await _roomRepo.GetQueryable()
                    .CountAsync(r => r.RoomTypeId == dto.RoomTypeId &&
                                     r.HotelId == user.HotelId);

                var maxInventory = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == dto.RoomTypeId)
                    .MaxAsync(i => (int?)i.TotalInventory);

                if (maxInventory == null)
                    throw new NotFoundException("Inventory not defined");

                if (currentCount >= maxInventory)
                    throw new ConflictException($"Max rooms allowed: {maxInventory}");

                // Create Room
                var room = new Room
                {
                    RoomId = Guid.NewGuid(),
                    RoomNumber = dto.RoomNumber,
                    Floor = dto.Floor,
                    HotelId = user.HotelId.Value,
                    RoomTypeId = dto.RoomTypeId,
                    IsActive = true
                };

                await _roomRepo.AddAsync(room);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region UPDATE

        public async Task UpdateRoomAsync(Guid userId, UpdateRoomDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized");

                var room = await _roomRepo.GetQueryable()
                    .FirstOrDefaultAsync(r =>
                        r.RoomId == dto.RoomId &&
                        r.HotelId == user.HotelId);

                if (room == null)
                    throw new NotFoundException("Room not found");

                var validRoomType = await _roomTypeRepo.GetQueryable()
                    .AnyAsync(rt =>
                        rt.RoomTypeId == dto.RoomTypeId &&
                        rt.HotelId == user.HotelId);

                if (!validRoomType)
                    throw new NotFoundException("Invalid RoomType");

                room.RoomNumber = dto.RoomNumber;
                room.Floor = dto.Floor;
                room.RoomTypeId = dto.RoomTypeId;

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region TOGGLE STATUS

        public async Task ToggleRoomStatusAsync(Guid userId, Guid roomId, bool isActive)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized");

            var room = await _roomRepo.GetQueryable()
                .FirstOrDefaultAsync(r =>
                    r.RoomId == roomId &&
                    r.HotelId == user.HotelId);

            if (room == null)
                throw new NotFoundException("Room not found");

            room.IsActive = isActive;

            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region GET ROOMS 

        public async Task<IEnumerable<RoomListResponseDto>> GetRoomsByHotelAsync(
            Guid userId, int pageNumber, int pageSize)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized");

            if (user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized");

            var query = _roomRepo.GetQueryable()
                .Include(r => r.RoomType)
                .Where(r => r.HotelId == user.HotelId)
                .OrderBy(r => r.RoomNumber);

            var rooms = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return rooms.Select(r => new RoomListResponseDto
            {
                RoomId = r.RoomId,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeName = r.RoomType!.Name,
                IsActive = r.IsActive
            });
        }

        #endregion
    }
}
