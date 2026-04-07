using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
    private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
    private readonly Mock<IRepository<Guid, Review>> _reviewRepoMock = new();
    private readonly Mock<IRepository<Guid, Room>> _roomRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();

    private DashboardService CreateSut() => new(
        _userRepoMock.Object, _hotelRepoMock.Object, _reservationRepoMock.Object,
        _transactionRepoMock.Object, _reviewRepoMock.Object,
        _roomRepoMock.Object, _roomTypeRepoMock.Object);

    private static Hotel MakeHotel(Guid hotelId) => new()
    {
        HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C",
        ContactNumber = "1234567890", IsActive = true, CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAdminDashboardAsync_ValidAdmin_ReturnsDashboard()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var users = new List<User> { new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
        _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(MakeHotel(hotelId));
        _roomRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Room>().AsQueryable().BuildMock());
        _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomType>().AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetAdminDashboardAsync(adminId);

        // Assert
        result.HotelName.Should().Be("Grand Hotel");
        result.TotalReservations.Should().Be(0);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_AdminHasNoHotel_ThrowsNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var users = new List<User> { new() { UserId = adminId, HotelId = null, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetAdminDashboardAsync(adminId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetGuestDashboardAsync_ValidGuest_ReturnsDashboard()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetGuestDashboardAsync(guestId);

        // Assert
        result.TotalBookings.Should().Be(0);
        result.TotalSpent.Should().Be(0);
    }

    [Fact]
    public async Task GetSuperAdminDashboardAsync_ReturnsAggregatedStats()
    {
        // Arrange
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Hotel> { MakeHotel(Guid.NewGuid()) }.AsQueryable().BuildMock());
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetSuperAdminDashboardAsync();

        // Assert
        result.TotalHotels.Should().Be(1);
        result.TotalRevenue.Should().Be(0);
    }
}
