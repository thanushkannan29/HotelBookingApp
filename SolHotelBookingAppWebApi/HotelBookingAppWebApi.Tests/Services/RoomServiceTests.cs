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
    private readonly Mock<IRepository<Guid, Room>> _roomRepo = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepo = new();
    private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RoomService _sut;

    public RoomServiceTests()
    {
        _sut = new RoomService(
            _roomRepo.Object, _roomTypeRepo.Object, _inventoryRepo.Object,
            _userRepo.Object, _auditLogService.Object, _unitOfWork.Object);
    }

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com",
        HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    // ── AddRoomAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRoomAsync_ValidInput_AddsRoom()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomTypeId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType>
        {
            new() { RoomTypeId = roomTypeId, HotelId = admin.HotelId!.Value, Name = "Deluxe", IsActive = true }
        }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        var existingRooms = new List<Room>().AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(existingRooms);

        var inventories = new List<RoomTypeInventory>
        {
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = DateOnly.FromDateTime(DateTime.Now), TotalInventory = 5, ReservedInventory = 0 }
        }.AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);
        _roomRepo.Setup(r => r.AddAsync(It.IsAny<Room>())).ReturnsAsync((Room rm) => rm);

        var dto = new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId };

        // Act
        await _sut.AddRoomAsync(admin.UserId, dto);

        // Assert
        _roomRepo.Verify(r => r.AddAsync(It.Is<Room>(rm => rm.RoomNumber == "101")), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomAsync(Guid.NewGuid(), new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid() }))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomAsync(admin.UserId, new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid() }))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomAsync_InvalidRoomType_ThrowsNotFoundException()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType>().AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomAsync(admin.UserId, new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid() }))
            .Should().ThrowAsync<NotFoundException>().WithMessage("*RoomType*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomAsync_DuplicateRoomNumber_ThrowsConflict()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomTypeId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType>
        {
            new() { RoomTypeId = roomTypeId, HotelId = admin.HotelId!.Value, Name = "Deluxe", IsActive = true }
        }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        var existingRooms = new List<Room>
        {
            new() { RoomId = Guid.NewGuid(), RoomNumber = "101", HotelId = admin.HotelId.Value, RoomTypeId = roomTypeId, Floor = 1 }
        }.AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(existingRooms);

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomAsync(admin.UserId, new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId }))
            .Should().ThrowAsync<ConflictException>().WithMessage("*already exists*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomAsync_MaxInventoryReached_ThrowsConflict()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomTypeId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType>
        {
            new() { RoomTypeId = roomTypeId, HotelId = admin.HotelId!.Value, Name = "Deluxe", IsActive = true }
        }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // 1 existing room, max inventory = 1
        var existingRooms = new List<Room>
        {
            new() { RoomId = Guid.NewGuid(), RoomNumber = "100", HotelId = admin.HotelId.Value, RoomTypeId = roomTypeId, Floor = 1 }
        }.AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(existingRooms);

        var inventories = new List<RoomTypeInventory>
        {
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = DateOnly.FromDateTime(DateTime.Now), TotalInventory = 1, ReservedInventory = 0 }
        }.AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomAsync(admin.UserId, new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId }))
            .Should().ThrowAsync<ConflictException>().WithMessage("*Maximum*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── UpdateRoomAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoomAsync_ValidInput_UpdatesRoom()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomTypeId = Guid.NewGuid();
        var room = new Room { RoomId = Guid.NewGuid(), RoomNumber = "101", Floor = 1, HotelId = admin.HotelId!.Value, RoomTypeId = roomTypeId };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rooms = new List<Room> { room }.AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(rooms);

        var roomTypes = new List<RoomType>
        {
            new() { RoomTypeId = roomTypeId, HotelId = admin.HotelId.Value, Name = "Suite", IsActive = true }
        }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        var dto = new UpdateRoomDto { RoomId = room.RoomId, RoomNumber = "102", Floor = 2, RoomTypeId = roomTypeId };

        // Act
        await _sut.UpdateRoomAsync(admin.UserId, dto);

        // Assert
        room.RoomNumber.Should().Be("102");
        room.Floor.Should().Be(2);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRoomAsync_RoomNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rooms = new List<Room>().AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(rooms);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateRoomAsync(admin.UserId, new UpdateRoomDto { RoomId = Guid.NewGuid(), RoomNumber = "X", Floor = 1, RoomTypeId = Guid.NewGuid() }))
            .Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── ToggleRoomStatusAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ToggleRoomStatusAsync_ActiveRoom_DeactivatesIt()
    {
        // Arrange
        var admin = MakeAdmin();
        var room = new Room { RoomId = Guid.NewGuid(), RoomNumber = "101", Floor = 1, HotelId = admin.HotelId!.Value, RoomTypeId = Guid.NewGuid(), IsActive = true };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rooms = new List<Room> { room }.AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(rooms);

        // Act
        await _sut.ToggleRoomStatusAsync(admin.UserId, room.RoomId, false);

        // Assert
        room.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleRoomStatusAsync_RoomNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rooms = new List<Room>().AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(rooms);

        // Act & Assert
        await _sut.Invoking(s => s.ToggleRoomStatusAsync(admin.UserId, Guid.NewGuid(), false))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── GetRoomsByHotelAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRoomsByHotelAsync_ValidAdmin_ReturnsRooms()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomType = new RoomType { RoomTypeId = Guid.NewGuid(), HotelId = admin.HotelId!.Value, Name = "Deluxe", IsActive = true };
        var rooms = new List<Room>
        {
            new() { RoomId = Guid.NewGuid(), RoomNumber = "101", Floor = 1, HotelId = admin.HotelId.Value, RoomTypeId = roomType.RoomTypeId, IsActive = true, RoomType = roomType }
        }.AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(rooms);

        // Act
        var result = await _sut.GetRoomsByHotelAsync(admin.UserId, 1, 10);

        // Assert
        result.Should().HaveCount(1);
        result.First().RoomNumber.Should().Be("101");
    }

    [Fact]
    public async Task GetRoomsByHotelAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.GetRoomsByHotelAsync(Guid.NewGuid(), 1, 10))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetRoomCountByHotelAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetRoomCountByHotelAsync_ValidAdmin_ReturnsCount()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rooms = new List<Room>
        {
            new() { RoomId = Guid.NewGuid(), RoomNumber = "101", Floor = 1, HotelId = admin.HotelId!.Value, RoomTypeId = Guid.NewGuid() },
            new() { RoomId = Guid.NewGuid(), RoomNumber = "102", Floor = 1, HotelId = admin.HotelId.Value, RoomTypeId = Guid.NewGuid() }
        }.AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(rooms);

        // Act
        var result = await _sut.GetRoomCountByHotelAsync(admin.UserId);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetRoomCountByHotelAsync_AdminNoHotel_ReturnsZero()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act
        var result = await _sut.GetRoomCountByHotelAsync(admin.UserId);

        // Assert
        result.Should().Be(0);
    }
}
