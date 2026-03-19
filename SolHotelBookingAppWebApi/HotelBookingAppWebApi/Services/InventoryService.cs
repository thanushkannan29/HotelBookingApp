using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
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

        // ── ADD INVENTORY (DATE RANGE) ────────────────────────────────────────
        public async Task AddInventoryAsync(Guid userId, CreateInventoryDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var user = await _userRepo.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user?.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized.");

                var roomType = await _roomTypeRepo.FirstOrDefaultAsync(rt =>
                    rt.RoomTypeId == dto.RoomTypeId && rt.HotelId == user.HotelId)
                    ?? throw new NotFoundException("Invalid RoomType.");

                // Get existing dates in one query
                var existingDates = await _inventoryRepo.GetQueryable()
                    .Where(i => i.RoomTypeId == dto.RoomTypeId &&
                                i.Date >= dto.StartDate &&
                                i.Date <= dto.EndDate)
                    .Select(i => i.Date)
                    .ToListAsync();

                var existingDateSet = existingDates.ToHashSet();

                for (var date = dto.StartDate; date <= dto.EndDate; date = date.AddDays(1))
                {
                    if (!existingDateSet.Contains(date))
                    {
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

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── UPDATE INVENTORY ──────────────────────────────────────────────────
        public async Task UpdateInventoryAsync(Guid userId, UpdateInventoryDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var inventory = await _inventoryRepo.GetQueryable()
                    .Include(i => i.RoomType)
                    .FirstOrDefaultAsync(i => i.RoomTypeInventoryId == dto.RoomTypeInventoryId)
                    ?? throw new NotFoundException("Inventory not found.");

                if (dto.TotalInventory < inventory.ReservedInventory)
                    throw new InsufficientInventoryException(
                        "Cannot reduce total inventory below reserved inventory.");

                inventory.TotalInventory = dto.TotalInventory;
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        // ── GET INVENTORY ─────────────────────────────────────────────────────
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
                    ReservedInventory = i.ReservedInventory
                })
                .ToListAsync();
        }
    }
}
