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
    /// <summary>
    /// Manages physical hotel rooms — creation, updates, status toggling, and listing.
    /// Room count is capped by the room type's configured inventory.
    /// </summary>
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

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task AddRoomAsync(Guid userId, CreateRoomDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var hotelId = await GetAdminHotelIdOrThrowAsync(userId);
                await ValidateRoomTypeOwnershipAsync(dto.RoomTypeId, hotelId);
                await EnsureRoomNumberIsUniqueAsync(hotelId, dto.RoomNumber);
                await EnsureRoomCapacityNotExceededAsync(dto.RoomTypeId, hotelId);

                var room = BuildRoom(dto, hotelId);
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

        public async Task UpdateRoomAsync(Guid userId, UpdateRoomDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await GetUserOrThrowAsync(userId);
                var room = await GetRoomForHotelOrThrowAsync(dto.RoomId, user.HotelId);
                await ValidateRoomTypeOwnershipAsync(dto.RoomTypeId, user.HotelId!.Value);

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

        public async Task ToggleRoomStatusAsync(Guid userId, Guid roomId, bool isActive)
        {
            var user = await GetUserOrThrowAsync(userId);
            var room = await GetRoomForHotelOrThrowAsync(roomId, user.HotelId);
            room.IsActive = isActive;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<RoomListResponseDto>> GetRoomsByHotelAsync(
            Guid userId, int pageNumber, int pageSize)
        {
            var hotelId = await GetAdminHotelIdOrThrowAsync(userId);
            var rooms = await _roomRepo.GetQueryable()
                .Include(r => r.RoomType)
                .Where(r => r.HotelId == hotelId)
                .OrderBy(r => r.RoomNumber)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return rooms.Select(MapToDto);
        }

        public async Task<int> GetRoomCountByHotelAsync(Guid userId)
        {
            var user = await GetUserOrThrowAsync(userId);
            if (user.HotelId is null) return 0;
            return await _roomRepo.GetQueryable().CountAsync(r => r.HotelId == user.HotelId);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<Guid> GetAdminHotelIdOrThrowAsync(Guid userId)
        {
            var user = await GetUserOrThrowAsync(userId);
            if (user.HotelId is null) throw new UnAuthorizedException("Unauthorized.");
            return user.HotelId.Value;
        }

        private async Task<User> GetUserOrThrowAsync(Guid userId)
            => await _userRepo.GetAsync(userId) ?? throw new UnAuthorizedException("Unauthorized.");

        private async Task<Room> GetRoomForHotelOrThrowAsync(Guid roomId, Guid? hotelId)
            => await _roomRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.HotelId == hotelId)
                ?? throw new NotFoundException("Room not found.");

        private async Task ValidateRoomTypeOwnershipAsync(Guid roomTypeId, Guid hotelId)
        {
            var exists = await _roomTypeRepo.GetQueryable()
                .AnyAsync(rt => rt.RoomTypeId == roomTypeId && rt.HotelId == hotelId);
            if (!exists) throw new NotFoundException("Invalid RoomType.");
        }

        private async Task EnsureRoomNumberIsUniqueAsync(Guid hotelId, string roomNumber)
        {
            var exists = await _roomRepo.GetQueryable()
                .AnyAsync(r => r.HotelId == hotelId && r.RoomNumber == roomNumber);
            if (exists) throw new ConflictException("Room number already exists.");
        }

        private async Task EnsureRoomCapacityNotExceededAsync(Guid roomTypeId, Guid hotelId)
        {
            var currentCount = await _roomRepo.GetQueryable()
                .CountAsync(r => r.RoomTypeId == roomTypeId && r.HotelId == hotelId);

            var maxInventory = await _inventoryRepo.GetQueryable()
                .Where(i => i.RoomTypeId == roomTypeId)
                .MaxAsync(i => (int?)i.TotalInventory);

            if (maxInventory is null)
                throw new NotFoundException("Inventory not defined for this room type.");
            if (currentCount >= maxInventory)
                throw new ConflictException($"Maximum rooms allowed for this type: {maxInventory}.");
        }

        private static Room BuildRoom(CreateRoomDto dto, Guid hotelId) => new()
        {
            RoomId = Guid.NewGuid(),
            RoomNumber = dto.RoomNumber,
            Floor = dto.Floor,
            HotelId = hotelId,
            RoomTypeId = dto.RoomTypeId,
            IsActive = true
        };

        private static RoomListResponseDto MapToDto(Room room) => new()
        {
            RoomId = room.RoomId,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = room.RoomType!.Name,
            IsActive = room.IsActive
        };
    }
}
