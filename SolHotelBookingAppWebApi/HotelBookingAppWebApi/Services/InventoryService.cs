using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using Microsoft.EntityFrameworkCore;
namespace HotelBookingAppWebApi.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly HotelBookingContext _context;

        public InventoryService(HotelBookingContext context)
        {
            _context = context;
        }

        // ADD INVENTORY (DATE RANGE)

        public async Task AddInventoryAsync(Guid userId, CreateInventoryDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user?.HotelId == null)
                    throw new UnAuthorizedException("Unauthorized");

                var roomType = await _context.RoomTypes
                    .FirstOrDefaultAsync(rt => rt.RoomTypeId == dto.RoomTypeId
                                            && rt.HotelId == user.HotelId);

                if (roomType == null)
                    throw new NotFoundException("Invalid RoomType");

                for (var date = dto.StartDate; date <= dto.EndDate; date = date.AddDays(1))
                {
                    var exists = await _context.RoomTypeInventories
                        .FirstOrDefaultAsync(i => i.RoomTypeId == dto.RoomTypeId
                                               && i.Date == date);

                    if (exists == null)
                    {
                        await _context.RoomTypeInventories.AddAsync(new RoomTypeInventory
                        {
                            RoomTypeInventoryId = Guid.NewGuid(),
                            RoomTypeId = dto.RoomTypeId,
                            Date = date,
                            TotalInventory = dto.TotalInventory,
                            ReservedInventory = 0
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // UPDATE INVENTORY 

        public async Task UpdateInventoryAsync(Guid userId, UpdateInventoryDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var inventory = await _context.RoomTypeInventories
                    .Include(i => i.RoomType)
                    .FirstOrDefaultAsync(i => i.RoomTypeInventoryId == dto.RoomTypeInventoryId);

                if (inventory == null)
                    throw new InsufficientInventoryException("Inventory not found");

                if (dto.TotalInventory < inventory.ReservedInventory)
                    throw new InsufficientInventoryException("Cannot reduce below reserved inventory");

                inventory.TotalInventory = dto.TotalInventory;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ADJUST RESERVED INVENTORY

        public async Task AdjustReservedInventoryAsync(Guid userId, AdjustReservedInventoryDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var inventory = await _context.RoomTypeInventories
                    .FirstOrDefaultAsync(i => i.RoomTypeId == dto.RoomTypeId
                                           && i.Date == dto.Date);

                if (inventory == null)
                    throw new NotFoundException("Inventory not found");

                var newReserved = inventory.ReservedInventory + dto.Quantity;

                if (newReserved < 0)
                    throw new InsufficientInventoryException("Reserved inventory cannot be negative");

                if (newReserved > inventory.TotalInventory)
                    throw new InsufficientInventoryException("Overbooking detected");

                inventory.ReservedInventory = newReserved;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // GET INVENTORY 

        public async Task<IEnumerable<InventoryResponseDto>> GetInventoryAsync(
            Guid userId, Guid roomTypeId, DateOnly start, DateOnly end)
        {
            var result = await _context.RoomTypeInventories
                .FromSqlRaw("EXEC proc_GetInventoryByRoomType {0},{1},{2}",
                    roomTypeId, start, end)
                .ToListAsync();

            return result.Select(i => new InventoryResponseDto
            {
                RoomTypeInventoryId = i.RoomTypeInventoryId,
                Date = i.Date,
                TotalInventory = i.TotalInventory,
                ReservedInventory = i.ReservedInventory
            });
        }
    }
}
