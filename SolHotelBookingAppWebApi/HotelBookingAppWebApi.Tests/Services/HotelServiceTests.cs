using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class HotelServiceTests
{
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
    private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
    private readonly Mock<IAuditLogService> _auditLogMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private HotelService CreateSut() => new(
        _hotelRepoMock.Object, _userRepoMock.Object, _roomTypeRepoMock.Object,
        _transactionRepoMock.Object, _reservationRepoMock.Object,
        _auditLogMock.Object, _unitOfWorkMock.Object);

    private static Hotel MakeHotel(bool isActive = true) => new()
    {
        HotelId = Guid.NewGuid(), Name = "Grand Hotel", Address = "123 Main",
        City = "Mumbai", State = "MH", ContactNumber = "9999999999",
        IsActive = isActive, CreatedAt = DateTime.UtcNow,
        RoomTypes = new List<RoomType>(), Reviews = new List<Review>()
    };

    [Fact]
    public async Task GetTopHotelsAsync_ReturnsActiveHotels()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel(false) }.AsQueryable().BuildMock();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateSut();

        // Act
        var result = await sut.GetTopHotelsAsync();

        // Assert
        result.Should().HaveCount(1); // only active
    }

    [Fact]
    public async Task GetCitiesAsync_ReturnsDistinctCities()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel() }.AsQueryable().BuildMock();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateSut();

        // Act
        var result = await sut.GetCitiesAsync();

        // Assert
        result.Should().Contain("Mumbai");
    }

    [Fact]
    public async Task GetHotelsByCityAsync_ValidCity_ReturnsHotels()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel() }.AsQueryable().BuildMock();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateSut();

        // Act
        var result = await sut.GetHotelsByCityAsync("Mumbai");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_ValidHotel_ReturnsDetails()
    {
        // Arrange
        var hotel = MakeHotel();
        hotel.RoomTypes = new List<RoomType>();
        hotel.Reviews = new List<Review>();
        var hotels = new List<Hotel> { hotel }.AsQueryable().BuildMock();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateSut();

        // Act
        var result = await sut.GetHotelDetailsAsync(hotel.HotelId);

        // Assert
        result.Name.Should().Be("Grand Hotel");
    }

    [Fact]
    public async Task GetHotelDetailsAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Hotel>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetHotelDetailsAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateHotelAsync_ValidAdmin_UpdatesHotel()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = hotelId, CreatedAt = DateTime.UtcNow };
        var hotel = MakeHotel();
        hotel.HotelId = hotelId;
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var sut = CreateSut();

        // Act
        await sut.UpdateHotelAsync(admin.UserId, new UpdateHotelDto { Name = "Updated Hotel", Address = "456 New St", City = "Delhi", ContactNumber = "8888888888" });

        // Assert
        hotel.Name.Should().Be("Updated Hotel");
    }

    [Fact]
    public async Task ToggleHotelStatusAsync_BlockedHotel_ThrowsValidationException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = hotelId, CreatedAt = DateTime.UtcNow };
        var hotel = MakeHotel();
        hotel.HotelId = hotelId;
        hotel.IsBlockedBySuperAdmin = true;
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.ToggleHotelStatusAsync(admin.UserId, true);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task BlockHotelAsync_ValidHotel_BlocksHotel()
    {
        // Arrange
        var hotel = MakeHotel();
        _hotelRepoMock.Setup(r => r.GetAsync(hotel.HotelId)).ReturnsAsync(hotel);
        var sut = CreateSut();

        // Act
        await sut.BlockHotelAsync(hotel.HotelId);

        // Assert
        hotel.IsBlockedBySuperAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task BlockHotelAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _hotelRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Hotel?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.BlockHotelAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UnblockHotelAsync_ValidHotel_UnblocksHotel()
    {
        // Arrange
        var hotel = MakeHotel();
        hotel.IsBlockedBySuperAdmin = true;
        _hotelRepoMock.Setup(r => r.GetAsync(hotel.HotelId)).ReturnsAsync(hotel);
        var sut = CreateSut();

        // Act
        await sut.UnblockHotelAsync(hotel.HotelId);

        // Assert
        hotel.IsBlockedBySuperAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllHotelsForSuperAdminPagedAsync_ReturnsPagedHotels()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel() }.AsQueryable().BuildMock();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(hotels);
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllHotelsForSuperAdminPagedAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateHotelGstAsync_ValidPercent_UpdatesGst()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = hotelId, CreatedAt = DateTime.UtcNow };
        var hotel = MakeHotel();
        hotel.HotelId = hotelId;
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var sut = CreateSut();

        // Act
        await sut.UpdateHotelGstAsync(admin.UserId, 18m);

        // Assert
        hotel.GstPercent.Should().Be(18m);
    }
}
