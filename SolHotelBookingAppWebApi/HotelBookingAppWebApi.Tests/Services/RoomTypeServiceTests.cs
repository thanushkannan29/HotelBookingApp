using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class RoomTypeServiceTests : IDisposable
{
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepo = new();
    private readonly Mock<IRepository<Guid, RoomTypeRate>> _rateRepo = new();
    private readonly Mock<IRepository<Guid, RoomTypeAmenity>> _roomTypeAmenityRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly HotelBookingContext _context;
    private readonly RoomTypeService _sut;

    public RoomTypeServiceTests()
    {
        var options = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new HotelBookingContext(options);
        _sut = new RoomTypeService(
            _roomTypeRepo.Object, _rateRepo.Object, _roomTypeAmenityRepo.Object,
            _userRepo.Object, _auditLog.Object, _unitOfWork.Object, _context);
    }

    public void Dispose() => _context.Dispose();

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com",
        HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    private static RoomType MakeRoomType(Guid hotelId) => new()
    {
        RoomTypeId = Guid.NewGuid(), HotelId = hotelId,
        Name = "Deluxe", Description = "Nice room", MaxOccupancy = 2, IsActive = true
    };

    private static RoomTypeRate MakeRate(Guid roomTypeId) => new()
    {
        RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId,
        StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 30), Rate = 1500m
    };

    // ── AddRoomTypeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddRoomTypeAsync_ValidAdmin_AddsRoomType()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepo.Setup(r => r.AddAsync(It.IsAny<RoomType>())).ReturnsAsync((RoomType rt) => rt);

        var dto = new CreateRoomTypeDto { Name = "Suite", Description = "Luxury", MaxOccupancy = 4 };

        // Act
        await _sut.AddRoomTypeAsync(admin.UserId, dto);

        // Assert
        _roomTypeRepo.Verify(r => r.AddAsync(It.Is<RoomType>(rt => rt.Name == "Suite")), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomTypeAsync_WithAmenityIds_AddsAmenityAssociations()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepo.Setup(r => r.AddAsync(It.IsAny<RoomType>())).ReturnsAsync((RoomType rt) => rt);
        _roomTypeAmenityRepo.Setup(r => r.AddAsync(It.IsAny<RoomTypeAmenity>())).ReturnsAsync(new RoomTypeAmenity());

        var dto = new CreateRoomTypeDto
        {
            Name = "Suite", Description = "Luxury", MaxOccupancy = 4,
            AmenityIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        await _sut.AddRoomTypeAsync(admin.UserId, dto);

        // Assert
        _roomTypeAmenityRepo.Verify(r => r.AddAsync(It.IsAny<RoomTypeAmenity>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddRoomTypeAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomTypeAsync(Guid.NewGuid(), new CreateRoomTypeDto { Name = "X", MaxOccupancy = 1 }))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomTypeAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomTypeAsync(admin.UserId, new CreateRoomTypeDto { Name = "X", MaxOccupancy = 1 }))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRoomTypeAsync_OnException_Rollback()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepo.Setup(r => r.AddAsync(It.IsAny<RoomType>())).ThrowsAsync(new Exception("db error"));

        // Act & Assert
        await _sut.Invoking(s => s.AddRoomTypeAsync(admin.UserId, new CreateRoomTypeDto { Name = "X", MaxOccupancy = 1 }))
            .Should().ThrowAsync<Exception>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── UpdateRoomTypeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoomTypeAsync_ValidInput_UpdatesRoomType()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomType = MakeRoomType(admin.HotelId!.Value);
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType> { roomType }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        var dto = new UpdateRoomTypeDto
        {
            RoomTypeId = roomType.RoomTypeId, Name = "Updated Suite",
            Description = "Updated", MaxOccupancy = 3
        };

        // Act
        await _sut.UpdateRoomTypeAsync(admin.UserId, dto);

        // Assert
        roomType.Name.Should().Be("Updated Suite");
        roomType.MaxOccupancy.Should().Be(3);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRoomTypeAsync_WithAmenityIds_ReplacesAmenities()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomType = MakeRoomType(admin.HotelId!.Value);
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType> { roomType }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);
        _roomTypeAmenityRepo.Setup(r => r.AddAsync(It.IsAny<RoomTypeAmenity>())).ReturnsAsync(new RoomTypeAmenity());

        var dto = new UpdateRoomTypeDto
        {
            RoomTypeId = roomType.RoomTypeId, Name = "Suite", MaxOccupancy = 2,
            AmenityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        await _sut.UpdateRoomTypeAsync(admin.UserId, dto);

        // Assert
        _roomTypeAmenityRepo.Verify(r => r.AddAsync(It.IsAny<RoomTypeAmenity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRoomTypeAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateRoomTypeAsync(Guid.NewGuid(), new UpdateRoomTypeDto { RoomTypeId = Guid.NewGuid(), Name = "X" }))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task UpdateRoomTypeAsync_RoomTypeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType>().AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateRoomTypeAsync(admin.UserId, new UpdateRoomTypeDto { RoomTypeId = Guid.NewGuid(), Name = "X" }))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── ToggleRoomTypeStatusAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ToggleRoomTypeStatusAsync_ActiveRoomType_Deactivates()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomType = MakeRoomType(admin.HotelId!.Value);
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType> { roomType }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act
        await _sut.ToggleRoomTypeStatusAsync(admin.UserId, roomType.RoomTypeId, false);

        // Assert
        roomType.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleRoomTypeStatusAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.ToggleRoomTypeStatusAsync(Guid.NewGuid(), Guid.NewGuid(), false))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task ToggleRoomTypeStatusAsync_RoomTypeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType>().AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act & Assert
        await _sut.Invoking(s => s.ToggleRoomTypeStatusAsync(admin.UserId, Guid.NewGuid(), false))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── AddRateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRateAsync_ValidInput_AddsRate()
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

        var rates = new List<RoomTypeRate>().AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(rates);
        _rateRepo.Setup(r => r.AddAsync(It.IsAny<RoomTypeRate>())).ReturnsAsync(new RoomTypeRate());

        var dto = new CreateRoomTypeRateDto
        {
            RoomTypeId = roomTypeId,
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31),
            Rate = 2000m
        };

        // Act
        await _sut.AddRateAsync(admin.UserId, dto);

        // Assert
        _rateRepo.Verify(r => r.AddAsync(It.Is<RoomTypeRate>(rt => rt.Rate == 2000m)), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRateAsync_StartDateAfterEndDate_ThrowsValidation()
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

        var dto = new CreateRoomTypeRateDto
        {
            RoomTypeId = roomTypeId,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 7, 1), // end before start
            Rate = 1000m
        };

        // Act & Assert
        await _sut.Invoking(s => s.AddRateAsync(admin.UserId, dto))
            .Should().ThrowAsync<ValidationException>().WithMessage("*before end date*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRateAsync_OverlappingRate_ThrowsConflict()
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

        // Existing rate overlaps
        var existingRates = new List<RoomTypeRate>
        {
            new() { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31), Rate = 1500m }
        }.AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(existingRates);

        var dto = new CreateRoomTypeRateDto
        {
            RoomTypeId = roomTypeId,
            StartDate = new DateOnly(2026, 7, 15),
            EndDate = new DateOnly(2026, 8, 15),
            Rate = 2000m
        };

        // Act & Assert
        await _sut.Invoking(s => s.AddRateAsync(admin.UserId, dto))
            .Should().ThrowAsync<ConflictException>().WithMessage("*already exists*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRateAsync_RoomTypeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomTypes = new List<RoomType>().AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        var dto = new CreateRoomTypeRateDto
        {
            RoomTypeId = Guid.NewGuid(),
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31),
            Rate = 1000m
        };

        // Act & Assert
        await _sut.Invoking(s => s.AddRateAsync(admin.UserId, dto))
            .Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── UpdateRateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRateAsync_ValidInput_UpdatesRate()
    {
        // Arrange
        var admin = MakeAdmin();
        var rate = MakeRate(Guid.NewGuid());
        rate.RoomType = new RoomType { RoomTypeId = rate.RoomTypeId, HotelId = admin.HotelId!.Value, Name = "Deluxe", IsActive = true };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rates = new List<RoomTypeRate> { rate }.AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(rates);

        var dto = new UpdateRoomTypeRateDto
        {
            RoomTypeRateId = rate.RoomTypeRateId,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 31),
            Rate = 2500m
        };

        // Act
        await _sut.UpdateRateAsync(admin.UserId, dto);

        // Assert
        rate.Rate.Should().Be(2500m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRateAsync_RateNotFound_ThrowsUnauthorized()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rates = new List<RoomTypeRate>().AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(rates);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateRateAsync(admin.UserId, new UpdateRoomTypeRateDto { RoomTypeRateId = Guid.NewGuid(), Rate = 100m }))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task UpdateRateAsync_WrongHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = MakeAdmin();
        var rate = MakeRate(Guid.NewGuid());
        rate.RoomType = new RoomType { RoomTypeId = rate.RoomTypeId, HotelId = Guid.NewGuid(), Name = "X", IsActive = true }; // different hotel
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rates = new List<RoomTypeRate> { rate }.AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(rates);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateRateAsync(admin.UserId, new UpdateRoomTypeRateDto { RoomTypeRateId = rate.RoomTypeRateId, Rate = 100m }))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetRateByDateAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateByDateAsync_DateInRange_ReturnsRate()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        var rate = new RoomTypeRate
        {
            RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId,
            StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 30), Rate = 1500m
        };
        var rates = new List<RoomTypeRate> { rate }.AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(rates);

        var dto = new GetRateByDateRequestDto { RoomTypeId = roomTypeId, Date = new DateOnly(2026, 6, 15) };

        // Act
        var result = await _sut.GetRateByDateAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().Be(1500m);
    }

    [Fact]
    public async Task GetRateByDateAsync_DateOutOfRange_ThrowsNotFoundException()
    {
        // Arrange
        var rates = new List<RoomTypeRate>().AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(rates);

        var dto = new GetRateByDateRequestDto { RoomTypeId = Guid.NewGuid(), Date = new DateOnly(2026, 12, 1) };

        // Act & Assert
        await _sut.Invoking(s => s.GetRateByDateAsync(Guid.NewGuid(), dto))
            .Should().ThrowAsync<NotFoundException>().WithMessage("*Rate not found*");
    }

    // ── GetRoomTypesByHotelAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetRoomTypesByHotelAsync_ValidAdmin_ReturnsRoomTypes()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomType = MakeRoomType(admin.HotelId!.Value);
        roomType.RoomTypeAmenities = new List<RoomTypeAmenity>();
        roomType.Rooms = new List<Room>();
        var roomTypes = new List<RoomType> { roomType }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act
        var result = await _sut.GetRoomTypesByHotelAsync(admin.UserId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Deluxe");
    }

    [Fact]
    public async Task GetRoomTypesByHotelAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.GetRoomTypesByHotelAsync(Guid.NewGuid()))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetRoomTypesByHotelAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.GetRoomTypesByHotelAsync(admin.UserId))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetRoomTypesByHotelPagedAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetRoomTypesByHotelPagedAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        var admin = MakeAdmin();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var roomType = MakeRoomType(admin.HotelId!.Value);
        roomType.RoomTypeAmenities = new List<RoomTypeAmenity>();
        roomType.Rooms = new List<Room>();
        var roomTypes = new List<RoomType> { roomType }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act
        var result = await _sut.GetRoomTypesByHotelPagedAsync(admin.UserId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.RoomTypes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRoomTypesByHotelPagedAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.GetRoomTypesByHotelPagedAsync(admin.UserId, 1, 10))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetRatesAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRatesAsync_ValidAdmin_ReturnsRates()
    {
        // Arrange
        var admin = MakeAdmin();
        var roomTypeId = Guid.NewGuid();
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        var rates = new List<RoomTypeRate> { MakeRate(roomTypeId) }.AsQueryable().BuildMock();
        _rateRepo.Setup(r => r.GetQueryable()).Returns(rates);

        // Act
        var result = await _sut.GetRatesAsync(admin.UserId, roomTypeId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Rate.Should().Be(1500m);
    }

    [Fact]
    public async Task GetRatesAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.GetRatesAsync(Guid.NewGuid(), Guid.NewGuid()))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetRatesAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.GetRatesAsync(admin.UserId, Guid.NewGuid()))
            .Should().ThrowAsync<UnAuthorizedException>();
    }
}
