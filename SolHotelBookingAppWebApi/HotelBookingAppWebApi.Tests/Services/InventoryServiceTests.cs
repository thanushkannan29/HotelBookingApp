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
    private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private InventoryService CreateSut() => new(
        _inventoryRepoMock.Object, _roomTypeRepoMock.Object,
        _userRepoMock.Object, _unitOfWorkMock.Object);

    private static User MakeAdmin(Guid hotelId) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId, CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddInventoryAsync_ValidDto_AddsInventory()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        _roomTypeRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomType, bool>>>())).ReturnsAsync(roomType);
        var emptyInventory = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        _inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(emptyInventory);
        _inventoryRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomTypeInventory>())).ReturnsAsync((RoomTypeInventory i) => i);
        var sut = CreateSut();
        var dto = new CreateInventoryDto
        {
            RoomTypeId = roomTypeId,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            TotalInventory = 5
        };

        // Act
        var act = async () => await sut.AddInventoryAsync(admin.UserId, dto);

        // Assert
        await act.Should().NotThrowAsync();
        _inventoryRepoMock.Verify(r => r.AddAsync(It.IsAny<RoomTypeInventory>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddInventoryAsync_AdminHasNoHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.AddInventoryAsync(admin.UserId, new CreateInventoryDto { RoomTypeId = Guid.NewGuid() });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_ValidDto_UpdatesInventory()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = inventoryId, RoomTypeId = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), TotalInventory = 10, ReservedInventory = 2 };
        var inventoryQuery = new List<RoomTypeInventory> { inventory }.AsQueryable().BuildMock();
        _inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(inventoryQuery);
        var sut = CreateSut();

        // Act
        await sut.UpdateInventoryAsync(Guid.NewGuid(), new UpdateInventoryDto { RoomTypeInventoryId = inventoryId, TotalInventory = 15 });

        // Assert
        inventory.TotalInventory.Should().Be(15);
    }

    [Fact]
    public async Task UpdateInventoryAsync_BelowReserved_ThrowsInsufficientInventoryException()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = inventoryId, RoomTypeId = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today), TotalInventory = 10, ReservedInventory = 5 };
        var inventoryQuery = new List<RoomTypeInventory> { inventory }.AsQueryable().BuildMock();
        _inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(inventoryQuery);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.UpdateInventoryAsync(Guid.NewGuid(), new UpdateInventoryDto { RoomTypeInventoryId = inventoryId, TotalInventory = 3 });

        // Assert
        await act.Should().ThrowAsync<InsufficientInventoryException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var emptyInventory = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        _inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(emptyInventory);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.UpdateInventoryAsync(Guid.NewGuid(), new UpdateInventoryDto { RoomTypeInventoryId = Guid.NewGuid(), TotalInventory = 5 });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task GetInventoryAsync_ValidQuery_ReturnsInventory()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);
        var inventory = new List<RoomTypeInventory>
        {
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = date, TotalInventory = 10, ReservedInventory = 2 }
        }.AsQueryable().BuildMock();
        _inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(inventory);
        var sut = CreateSut();

        // Act
        var result = await sut.GetInventoryAsync(Guid.NewGuid(), roomTypeId, date, date.AddDays(7));

        // Assert
        result.Should().HaveCount(1);
        result.First().Available.Should().Be(8);
    }
}
