using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class RoomServiceTests
{
    private readonly Mock<IRepository<Guid, Room>> _roomRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IAuditLogService> _auditLogMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private RoomService CreateSut() => new(
        _roomRepoMock.Object, _roomTypeRepoMock.Object, _inventoryRepoMock.Object,
        _userRepoMock.Object, _auditLogMock.Object, _unitOfWorkMock.Object);

    private static User MakeAdmin(Guid hotelId) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId, CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddRoomAsync_ValidDto_AddsRoom()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<RoomType> { new() { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true } }.AsQueryable().BuildMock());
        _roomRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Room>().AsQueryable().BuildMock());
        _inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<RoomTypeInventory> { new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = DateOnly.FromDateTime(DateTime.Today), TotalInventory = 10, ReservedInventory = 0 } }.AsQueryable().BuildMock());
        _roomRepoMock.Setup(r => r.AddAsync(It.IsAny<Room>())).ReturnsAsync((Room rm) => rm);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.AddRoomAsync(admin.UserId, new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId });

        // Assert
        await act.Should().NotThrowAsync();
        _roomRepoMock.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Once);
    }

    [Fact]
    public async Task AddRoomAsync_DuplicateRoomNumber_ThrowsConflictException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<RoomType> { new() { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true } }.AsQueryable().BuildMock());
        _roomRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<Room> { new() { RoomId = Guid.NewGuid(), RoomNumber = "101", HotelId = hotelId, RoomTypeId = roomTypeId, Floor = 1, IsActive = true } }.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.AddRoomAsync(admin.UserId, new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId });

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomAsync_AdminHasNoHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.AddRoomAsync(admin.UserId, new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid() });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleRoomStatusAsync_ValidRoom_TogglesStatus()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var room = new Room { RoomId = roomId, HotelId = hotelId, RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid(), IsActive = true };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Room> { room }.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        await sut.ToggleRoomStatusAsync(admin.UserId, roomId, false);

        // Assert
        room.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetRoomCountByHotelAsync_ValidAdmin_ReturnsCount()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<Room> { new() { RoomId = Guid.NewGuid(), HotelId = hotelId, RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid(), IsActive = true } }.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetRoomCountByHotelAsync(admin.UserId);

        // Assert
        result.Should().Be(1);
    }
}
