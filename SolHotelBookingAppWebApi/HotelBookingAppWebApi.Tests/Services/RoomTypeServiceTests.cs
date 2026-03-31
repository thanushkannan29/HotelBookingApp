using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class RoomTypeServiceTests
{
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomTypeRate>> _rateRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomTypeAmenity>> _roomTypeAmenityRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IAuditLogService> _auditLogMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private HotelBookingContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HotelBookingContext(options);
    }

    private RoomTypeService CreateSut(string dbName) => new(
        _roomTypeRepoMock.Object, _rateRepoMock.Object, _roomTypeAmenityRepoMock.Object,
        _userRepoMock.Object, _auditLogMock.Object, _unitOfWorkMock.Object, CreateContext(dbName));

    private static User MakeAdmin(Guid hotelId) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId, CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddRoomTypeAsync_ValidDto_AddsRoomType()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomType>())).ReturnsAsync((RoomType rt) => rt);
        var sut = CreateSut(nameof(AddRoomTypeAsync_ValidDto_AddsRoomType));

        // Act
        var act = async () => await sut.AddRoomTypeAsync(admin.UserId, new CreateRoomTypeDto { Name = "Deluxe", MaxOccupancy = 2 });

        // Assert
        await act.Should().NotThrowAsync();
        _roomTypeRepoMock.Verify(r => r.AddAsync(It.IsAny<RoomType>()), Times.Once);
    }

    [Fact]
    public async Task AddRoomTypeAsync_AdminHasNoHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        var sut = CreateSut(nameof(AddRoomTypeAsync_AdminHasNoHotel_ThrowsUnAuthorizedException));

        // Act
        var act = async () => await sut.AddRoomTypeAsync(admin.UserId, new CreateRoomTypeDto { Name = "Deluxe", MaxOccupancy = 2 });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleRoomTypeStatusAsync_ValidRoomType_TogglesStatus()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomType> { roomType }.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(ToggleRoomTypeStatusAsync_ValidRoomType_TogglesStatus));

        // Act
        await sut.ToggleRoomTypeStatusAsync(admin.UserId, roomTypeId, false);

        // Assert
        roomType.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AddRateAsync_ValidDto_AddsRate()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<RoomType> { new() { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true } }.AsQueryable().BuildMock());
        _rateRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomTypeRate>().AsQueryable().BuildMock());
        _rateRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomTypeRate>())).ReturnsAsync((RoomTypeRate rate) => rate);
        var sut = CreateSut(nameof(AddRateAsync_ValidDto_AddsRate));
        var dto = new CreateRoomTypeRateDto { RoomTypeId = roomTypeId, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)), Rate = 1500 };

        // Act
        var act = async () => await sut.AddRateAsync(admin.UserId, dto);

        // Assert
        await act.Should().NotThrowAsync();
        _rateRepoMock.Verify(r => r.AddAsync(It.IsAny<RoomTypeRate>()), Times.Once);
    }

    [Fact]
    public async Task AddRateAsync_OverlappingRange_ThrowsConflictException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<RoomType> { new() { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true } }.AsQueryable().BuildMock());
        var existingRate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)), Rate = 1000 };
        _rateRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomTypeRate> { existingRate }.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(AddRateAsync_OverlappingRange_ThrowsConflictException));
        var dto = new CreateRoomTypeRateDto { RoomTypeId = roomTypeId, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15)), Rate = 1500 };

        // Act
        var act = async () => await sut.AddRateAsync(admin.UserId, dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task GetRateByDateAsync_ValidDate_ReturnsRate()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(5));
        var rate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)), Rate = 1500 };
        _rateRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomTypeRate> { rate }.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(GetRateByDateAsync_ValidDate_ReturnsRate));

        // Act
        var result = await sut.GetRateByDateAsync(admin.UserId, new GetRateByDateRequestDto { RoomTypeId = roomTypeId, Date = date });

        // Assert
        result.Should().Be(1500m);
    }

    [Fact]
    public async Task GetRateByDateAsync_NoRateForDate_ThrowsNotFoundException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _rateRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomTypeRate>().AsQueryable().BuildMock());
        var sut = CreateSut(nameof(GetRateByDateAsync_NoRateForDate_ThrowsNotFoundException));

        // Act
        var act = async () => await sut.GetRateByDateAsync(admin.UserId, new GetRateByDateRequestDto { RoomTypeId = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today) });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateRoomTypeAsync_ValidDto_UpdatesRoomType()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Old", Description = "Old desc", MaxOccupancy = 2, IsActive = true };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { roomType }.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(UpdateRoomTypeAsync_ValidDto_UpdatesRoomType));
        var dto = new UpdateRoomTypeDto { RoomTypeId = roomTypeId, Name = "Updated", Description = "New desc", MaxOccupancy = 4 };

        // Act
        await sut.UpdateRoomTypeAsync(admin.UserId, dto);

        // Assert
        roomType.Name.Should().Be("Updated");
        roomType.MaxOccupancy.Should().Be(4);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRoomTypeAsync_UserNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var sut = CreateSut(nameof(UpdateRoomTypeAsync_UserNotFound_ThrowsUnAuthorizedException));

        // Act
        var act = async () => await sut.UpdateRoomTypeAsync(Guid.NewGuid(), new UpdateRoomTypeDto { RoomTypeId = Guid.NewGuid(), Name = "X", MaxOccupancy = 2 });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task UpdateRoomTypeAsync_RoomTypeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType>().AsQueryable().BuildMock());
        var sut = CreateSut(nameof(UpdateRoomTypeAsync_RoomTypeNotFound_ThrowsNotFoundException));

        // Act
        var act = async () => await sut.UpdateRoomTypeAsync(admin.UserId, new UpdateRoomTypeDto { RoomTypeId = Guid.NewGuid(), Name = "X", MaxOccupancy = 2 });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateRateAsync_ValidDto_UpdatesRate()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var rateId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var rate = new RoomTypeRate { RoomTypeRateId = rateId, RoomTypeId = roomTypeId, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), Rate = 1000, RoomType = roomType };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _rateRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomTypeRate> { rate }.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(UpdateRateAsync_ValidDto_UpdatesRate));
        var dto = new UpdateRoomTypeRateDto { RoomTypeRateId = rateId, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20)), Rate = 2000 };

        // Act
        await sut.UpdateRateAsync(admin.UserId, dto);

        // Assert
        rate.Rate.Should().Be(2000);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRateAsync_RateNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _rateRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomTypeRate>().AsQueryable().BuildMock());
        var sut = CreateSut(nameof(UpdateRateAsync_RateNotFound_ThrowsUnAuthorizedException));

        // Act
        var act = async () => await sut.UpdateRateAsync(admin.UserId, new UpdateRoomTypeRateDto { RoomTypeRateId = Guid.NewGuid(), Rate = 500 });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task AddRateAsync_StartAfterEnd_ThrowsValidationException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { new() { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true } }.AsQueryable().BuildMock());
        _rateRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomTypeRate>().AsQueryable().BuildMock());
        var sut = CreateSut(nameof(AddRateAsync_StartAfterEnd_ThrowsValidationException));
        var dto = new CreateRoomTypeRateDto { RoomTypeId = roomTypeId, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), EndDate = DateOnly.FromDateTime(DateTime.Today), Rate = 1500 };

        // Act
        var act = async () => await sut.AddRateAsync(admin.UserId, dto);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddRateAsync_RoomTypeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType>().AsQueryable().BuildMock());
        var sut = CreateSut(nameof(AddRateAsync_RoomTypeNotFound_ThrowsNotFoundException));
        var dto = new CreateRoomTypeRateDto { RoomTypeId = Guid.NewGuid(), StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), Rate = 1000 };

        // Act
        var act = async () => await sut.AddRateAsync(admin.UserId, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task GetRoomTypesByHotelAsync_ReturnsRoomTypes()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomType = new RoomType { RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Suite", MaxOccupancy = 3, IsActive = true, RoomTypeAmenities = new List<RoomTypeAmenity>(), Rooms = new List<Room>() };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { roomType }.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(GetRoomTypesByHotelAsync_ReturnsRoomTypes));

        // Act
        var result = await sut.GetRoomTypesByHotelAsync(admin.UserId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Suite");
    }

    [Fact]
    public async Task GetRoomTypesByHotelPagedAsync_ReturnsPaged()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomType = new RoomType { RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Suite", MaxOccupancy = 3, IsActive = true, RoomTypeAmenities = new List<RoomTypeAmenity>(), Rooms = new List<Room>() };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { roomType }.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(GetRoomTypesByHotelPagedAsync_ReturnsPaged));

        // Act
        var result = await sut.GetRoomTypesByHotelPagedAsync(admin.UserId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.RoomTypes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRatesAsync_ReturnsRatesForRoomType()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var rates = new List<RoomTypeRate>
        {
            new() { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)), Rate = 1500 }
        };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _rateRepoMock.Setup(r => r.GetQueryable())
            .Returns(rates.AsQueryable().BuildMock());
        var sut = CreateSut(nameof(GetRatesAsync_ReturnsRatesForRoomType));

        // Act
        var result = await sut.GetRatesAsync(admin.UserId, roomTypeId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Rate.Should().Be(1500);
    }

    [Fact]
    public async Task AddRoomTypeAsync_WithAmenityIds_SavesAmenityAssociations()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var amenityId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomType>())).ReturnsAsync((RoomType rt) => rt);
        _roomTypeAmenityRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomTypeAmenity>())).ReturnsAsync((RoomTypeAmenity rta) => rta);
        var sut = CreateSut(nameof(AddRoomTypeAsync_WithAmenityIds_SavesAmenityAssociations));
        var dto = new CreateRoomTypeDto { Name = "Deluxe", MaxOccupancy = 2, AmenityIds = new List<Guid> { amenityId } };

        // Act
        await sut.AddRoomTypeAsync(admin.UserId, dto);

        // Assert
        _roomTypeAmenityRepoMock.Verify(r => r.AddAsync(It.IsAny<RoomTypeAmenity>()), Times.Once);
    }

    [Fact]
    public async Task ToggleRoomTypeStatusAsync_RoomTypeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _roomTypeRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType>().AsQueryable().BuildMock());
        var sut = CreateSut(nameof(ToggleRoomTypeStatusAsync_RoomTypeNotFound_ThrowsNotFoundException));

        // Act
        var act = async () => await sut.ToggleRoomTypeStatusAsync(admin.UserId, Guid.NewGuid(), false);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
