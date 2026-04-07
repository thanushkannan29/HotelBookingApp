using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages room type inventory — adding date ranges, updating totals, and querying availability.
    /// Skips dates that already have inventory to support idempotent bulk inserts.
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly IRepository<Guid, RoomTypeInventory> _inventoryRepo;
        private readonly IRepository<Guid, RoomType> _roomTypeRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(
            IRepository<Guid, RoomTypeInventory> inventoryRepo,
            IRepository<Guid, RoomType> roomTypeRepo,
            IRepository<Guid, User> userRepo,
            IUnitOfWork unitOfWork)
        {
            _inventoryRepo = inventoryRepo;
            _roomTypeRepo = roomTypeRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task AddInventoryAsync(Guid userId, CreateInventoryDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await ValidateRoomTypeOwnershipAsync(userId, dto.RoomTypeId);
                var existingDates = await GetExistingDatesAsync(dto.RoomTypeId, dto.StartDate, dto.EndDate);
                await InsertMissingDatesAsync(dto, existingDates);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateInventoryAsync(Guid userId, UpdateInventoryDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var inventory = await GetInventoryOrThrowAsync(dto.RoomTypeInventoryId);
                EnsureTotalNotBelowReserved(inventory, dto.TotalInventory);
                inventory.TotalInventory = dto.TotalInventory;
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<InventoryResponseDto>> GetInventoryAsync(
            Guid userId, Guid roomTypeId, DateOnly start, DateOnly end)
        {
            return await _inventoryRepo.GetQueryable()
                .AsNoTracking()
                .Where(i => i.RoomTypeId == roomTypeId && i.Date >= start && i.Date <= end)
                .OrderBy(i => i.Date)
                .Select(i => new InventoryResponseDto
                {
                    RoomTypeInventoryId = i.RoomTypeInventoryId,
                    Date = i.Date,
                    TotalInventory = i.TotalInventory,
                    ReservedInventory = i.ReservedInventory,
                    Available = i.TotalInventory - i.ReservedInventory
                })
                .ToListAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task ValidateRoomTypeOwnershipAsync(Guid userId, Guid roomTypeId)
        {
            var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.HotelId is null) throw new UnAuthorizedException("Unauthorized.");

            var roomTypeExists = await _roomTypeRepo.FirstOrDefaultAsync(rt =>
                rt.RoomTypeId == roomTypeId && rt.HotelId == user.HotelId);
            if (roomTypeExists is null) throw new NotFoundException("Invalid RoomType.");
        }

        private async Task<HashSet<DateOnly>> GetExistingDatesAsync(
            Guid roomTypeId, DateOnly start, DateOnly end)
        {
            var dates = await _inventoryRepo.GetQueryable()
                .Where(i => i.RoomTypeId == roomTypeId && i.Date >= start && i.Date <= end)
                .Select(i => i.Date)
                .ToListAsync();
            return dates.ToHashSet();
        }

        private async Task InsertMissingDatesAsync(
            CreateInventoryDto dto, HashSet<DateOnly> existingDates)
        {
            for (var date = dto.StartDate; date <= dto.EndDate; date = date.AddDays(1))
            {
                if (existingDates.Contains(date)) continue;
                await _inventoryRepo.AddAsync(new RoomTypeInventory
                {
                    RoomTypeInventoryId = Guid.NewGuid(),
                    RoomTypeId = dto.RoomTypeId,
                    Date = date,
                    TotalInventory = dto.TotalInventory,
                    ReservedInventory = 0
                });
            }
        }

        private async Task<RoomTypeInventory> GetInventoryOrThrowAsync(Guid inventoryId)
            => await _inventoryRepo.GetQueryable()
                .Include(i => i.RoomType)
                .FirstOrDefaultAsync(i => i.RoomTypeInventoryId == inventoryId)
                ?? throw new NotFoundException("Inventory not found.");

        private static void EnsureTotalNotBelowReserved(RoomTypeInventory inventory, int newTotal)
        {
            if (newTotal < inventory.ReservedInventory)
                throw new InsufficientInventoryException(
                    "Cannot reduce total inventory below reserved inventory.");
        }
    }
}
