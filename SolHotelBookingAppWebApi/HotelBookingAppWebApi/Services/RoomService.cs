using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelBookingAppWebApi.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRepository<Guid, Room> _roomRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        public RoomService(
            IRepository<Guid, Room> roomRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, User> userRepo,
            IAuditLogService auditLogService,
            IUnitOfWork unitOfWork)
        {
            _roomRepo = roomRepo;
            _roomTypeRepo = roomTypeRepo;
            _inventoryRepo = inventoryRepo;
            _userRepo = userRepo;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        // ── ADD ROOM ──────────────────────────────────────────────────────────
        public async Task AddRoomAsync(Guid userId, CreateRoomDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                if (user.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized.");

                var roomTypeExists = await _roomTypeRepo.GetQueryable()
                    .AnyAsync(rt => rt.RoomTypeId == dto.RoomTypeId && rt.HotelId == user.HotelId);

                if (!roomTypeExists)
                    throw new NotFoundException("Invalid RoomType.");

                var exists = await _roomRepo.GetQueryable()
                    .AnyAsync(r => r.HotelId == user.HotelId && r.RoomNumber == dto.RoomNumber);

                if (exists)
                    throw new ConflictException("Room number already exists.");

                var currentCount = await _roomRepo.GetQueryable()
                    .CountAsync(r => r.RoomTypeId == dto.RoomTypeId && r.HotelId == user.HotelId);

                var maxInventory = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == dto.RoomTypeId)
                    .MaxAsync(i => (int?)i.TotalInventory);

                if (maxInventory == null)
                    throw new NotFoundException("Inventory not defined for this room type.");

                if (currentCount >= maxInventory)
                    throw new ConflictException($"Maximum rooms allowed for this type: {maxInventory}.");

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

                await _auditLogService.LogAsync(userId, "RoomAdded", "Room",
                    room.RoomId, JsonSerializer.Serialize(dto));
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── UPDATE ROOM ───────────────────────────────────────────────────────
        public async Task UpdateRoomAsync(Guid userId, UpdateRoomDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.GetAsync(userId)
                    ?? throw new UnAuthorizedException("Unauthorized.");

                var room = await _roomRepo.GetQueryable()
                    .FirstOrDefaultAsync(r => r.RoomId == dto.RoomId && r.HotelId == user.HotelId)
                    ?? throw new NotFoundException("Room not found.");

                var validRoomType = await _roomTypeRepo.GetQueryable()
                    .AnyAsync(rt => rt.RoomTypeId == dto.RoomTypeId && rt.HotelId == user.HotelId);

                if (!validRoomType)
                    throw new NotFoundException("Invalid RoomType.");

                var before = new { room.RoomNumber, room.Floor, room.RoomTypeId };

                room.RoomNumber = dto.RoomNumber;
                room.Floor = dto.Floor;
                room.RoomTypeId = dto.RoomTypeId;

                await _unitOfWork.CommitAsync();

                await _auditLogService.LogAsync(userId, "RoomUpdated", "Room",
                    room.RoomId, JsonSerializer.Serialize(new { Before = before, After = dto }));
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── TOGGLE ROOM STATUS ────────────────────────────────────────────────
        public async Task ToggleRoomStatusAsync(Guid userId, Guid roomId, bool isActive)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            var room = await _roomRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.HotelId == user.HotelId)
                ?? throw new NotFoundException("Room not found.");

            room.IsActive = isActive;
            await _unitOfWork.SaveChangesAsync();
        }

        // ── GET ROOMS ─────────────────────────────────────────────────────────
        public async Task<IEnumerable<RoomListResponseDto>> GetRoomsByHotelAsync(
            Guid userId, int pageNumber, int pageSize)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");

            if (user.HotelId == null)
                throw new UnAuthorizedException("Unauthorized.");

            var rooms = await _roomRepo.GetQueryable()
                .Include(r => r.RoomType)
                .Where(r => r.HotelId == user.HotelId)
                .OrderBy(r => r.RoomNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return rooms.Select(r => new RoomListResponseDto
            {
                RoomId = r.RoomId,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = r.RoomType!.Name,
                IsActive = r.IsActive
            });
        }

        public async Task<int> GetRoomCountByHotelAsync(Guid userId)
        {
            var user = await _userRepo.GetAsync(userId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (user.HotelId == null) return 0;
            return await _roomRepo.GetQueryable()
                .CountAsync(r => r.HotelId == user.HotelId);
        }
    }
}
