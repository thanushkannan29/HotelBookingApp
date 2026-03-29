using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class InventoryServiceTests
{
    private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepo = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _sut = new InventoryService(_inventoryRepo.Object, _roomTypeRepo.Object, _userRepo.Object, _unitOfWork.Object);
    }

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com",
        HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    // ── AddInventoryAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddInventoryAsync_NewDates_AddsInventoryForEachDay()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomTypeId = Guid.NewGuid();
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);

        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = admin.HotelId!.Value, Name = "Deluxe", IsActive = true };
        _roomTypeRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomType, bool>>>())).ReturnsAsync(roomType);

        var existingInventories = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(existingInventories);
        _inventoryRepo.Setup(r => r.AddAsync(It.IsAny<RoomTypeInventory>())).ReturnsAsync((RoomTypeInventory inv) => inv);

        var dto = new CreateInventoryDto
        {
            RoomTypeId = roomTypeId,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 3),
            TotalInventory = 5
        };

        // Act
        await _sut.AddInventoryAsync(admin.UserId, dto);

        // Assert
        // 3 days: June 1, 2, 3
        _inventoryRepo.Verify(r => r.AddAsync(It.IsAny<RoomTypeInventory>()), Times.Exactly(3));
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AddInventoryAsync_ExistingDatesSkipped_OnlyAddsNewDates()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomTypeId = Guid.NewGuid();
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);

        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = admin.HotelId!.Value, Name = "Deluxe", IsActive = true };
        _roomTypeRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomType, bool>>>())).ReturnsAsync(roomType);

        // June 1 already exists
        var existingInventories = new List<RoomTypeInventory>
        {
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = new DateOnly(2026, 6, 1), TotalInventory = 5, ReservedInventory = 0 }
        }.AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(existingInventories);
        _inventoryRepo.Setup(r => r.AddAsync(It.IsAny<RoomTypeInventory>())).ReturnsAsync((RoomTypeInventory inv) => inv);

        var dto = new CreateInventoryDto
        {
            RoomTypeId = roomTypeId,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 3),
            TotalInventory = 5
        };

        // Act
        await _sut.AddInventoryAsync(admin.UserId, dto);

        // Assert — only June 2 and 3 added
        _inventoryRepo.Verify(r => r.AddAsync(It.IsAny<RoomTypeInventory>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddInventoryAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.AddInventoryAsync(Guid.NewGuid(), new CreateInventoryDto
        {
            RoomTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 3),
            TotalInventory = 5
        })).Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddInventoryAsync_InvalidRoomType_ThrowsNotFoundException()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _roomTypeRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomType, bool>>>())).ReturnsAsync((RoomType?)null);

        // Act & Assert
        await _sut.Invoking(s => s.AddInventoryAsync(admin.UserId, new CreateInventoryDto
        {
            RoomTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 3),
            TotalInventory = 5
        })).Should().ThrowAsync<NotFoundException>().WithMessage("*RoomType*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── UpdateInventoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateInventoryAsync_ValidInput_UpdatesTotalInventory()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var inventory = new RoomTypeInventory
        {
            RoomTypeInventoryId = inventoryId,
            RoomTypeId = Guid.NewGuid(),
            Date = new DateOnly(2026, 6, 1),
            TotalInventory = 5,
            ReservedInventory = 2
        };
        var inventories = new List<RoomTypeInventory> { inventory }.AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);

        var dto = new UpdateInventoryDto { RoomTypeInventoryId = inventoryId, TotalInventory = 10 };

        // Act
        await _sut.UpdateInventoryAsync(Guid.NewGuid(), dto);

        // Assert
        inventory.TotalInventory.Should().Be(10);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_BelowReserved_ThrowsInsufficientInventory()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var inventory = new RoomTypeInventory
        {
            RoomTypeInventoryId = inventoryId,
            RoomTypeId = Guid.NewGuid(),
            Date = new DateOnly(2026, 6, 1),
            TotalInventory = 5,
            ReservedInventory = 4
        };
        var inventories = new List<RoomTypeInventory> { inventory }.AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);

        var dto = new UpdateInventoryDto { RoomTypeInventoryId = inventoryId, TotalInventory = 2 };

        // Act & Assert
        await _sut.Invoking(s => s.UpdateInventoryAsync(Guid.NewGuid(), dto))
            .Should().ThrowAsync<InsufficientInventoryException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var inventories = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateInventoryAsync(Guid.NewGuid(), new UpdateInventoryDto { RoomTypeInventoryId = Guid.NewGuid(), TotalInventory = 5 }))
            .Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── GetInventoryAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetInventoryAsync_ValidRange_ReturnsInventoryItems()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        var start = new DateOnly(2026, 6, 1);
        var end = new DateOnly(2026, 6, 3);
        var inventories = new List<RoomTypeInventory>
        {
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = start, TotalInventory = 5, ReservedInventory = 2 },
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = start.AddDays(1), TotalInventory = 5, ReservedInventory = 1 }
        }.AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);

        // Act
        var result = await _sut.GetInventoryAsync(Guid.NewGuid(), roomTypeId, start, end);

        // Assert
        result.Should().HaveCount(2);
        result.First().Available.Should().Be(3);
    }

    [Fact]
    public async Task GetInventoryAsync_NoInventory_ReturnsEmpty()
    {
        // Arrange
        var inventories = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);

        // Act
        var result = await _sut.GetInventoryAsync(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5));

        // Assert
        result.Should().BeEmpty();
    }
}
