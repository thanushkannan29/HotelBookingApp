using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepo = new();
    private readonly Mock<IRepository<Guid, Review>> _reviewRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepo.Object, _reservationRepo.Object, _reviewRepo.Object, _unitOfWork.Object);
    }

    private static User MakeUserWithDetails(Guid? userId = null) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        Name = "Alice",
        Email = "alice@test.com",
        Role = UserRole.Guest,
        CreatedAt = DateTime.UtcNow,
        UserDetails = new UserProfileDetails
        {
            UserDetailsId = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@test.com",
            PhoneNumber = "9999999999",
            Address = "123 Main St",
            City = "Chennai",
            State = "TN",
            Pincode = "600001",
            CreatedAt = DateTime.UtcNow
        }
    };

    // ── GetProfileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_UserWithDetails_ReturnsProfile()
    {
        // Arrange
        var user = MakeUserWithDetails();
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var reviews = new List<Review>().AsQueryable().BuildMock();
        _reviewRepo.Setup(r => r.GetQueryable()).Returns(reviews);

        // Act
        var result = await _sut.GetProfileAsync(user.UserId);

        // Assert
        result.Name.Should().Be("Alice");
        result.Email.Should().Be("alice@test.com");
        result.TotalReviewPoints.Should().Be(0);
    }

    [Fact]
    public async Task GetProfileAsync_UserWithReviews_CalculatesPoints()
    {
        // Arrange
        var user = MakeUserWithDetails();
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), UserId = user.UserId, HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 5, Comment = "Great", CreatedDate = DateTime.UtcNow },
            new() { ReviewId = Guid.NewGuid(), UserId = user.UserId, HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Good", CreatedDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _reviewRepo.Setup(r => r.GetQueryable()).Returns(reviews);

        // Act
        var result = await _sut.GetProfileAsync(user.UserId);

        // Assert
        result.TotalReviewPoints.Should().Be(200); // 2 reviews × 100 pts
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        await _sut.Invoking(s => s.GetProfileAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>().WithMessage("*User*");
    }

    [Fact]
    public async Task GetProfileAsync_UserWithoutDetails_AutoCreatesDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Name = "Bob", Email = "bob@test.com", Role = UserRole.Guest, CreatedAt = DateTime.UtcNow, UserDetails = null };
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var reviews = new List<Review>().AsQueryable().BuildMock();
        _reviewRepo.Setup(r => r.GetQueryable()).Returns(reviews);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Name.Should().Be("Bob");
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── UpdateProfileAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfileAsync_ValidInput_UpdatesProfile()
    {
        // Arrange
        var user = MakeUserWithDetails();
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var dto = new UpdateUserProfileDto { Name = "Alice Updated", PhoneNumber = "8888888888", City = "Mumbai" };

        // Act
        var result = await _sut.UpdateProfileAsync(user.UserId, dto);

        // Assert
        result.Name.Should().Be("Alice Updated");
        result.City.Should().Be("Mumbai");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateProfileAsync(Guid.NewGuid(), new UpdateUserProfileDto { Name = "X" }))
            .Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_NoDetails_ThrowsUserProfileException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Name = "Bob", Email = "bob@test.com", Role = UserRole.Guest, CreatedAt = DateTime.UtcNow, UserDetails = null };
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateProfileAsync(userId, new UpdateUserProfileDto { Name = "X" }))
            .Should().ThrowAsync<UserProfileException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_OnException_Rollback()
    {
        // Arrange
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateProfileAsync(Guid.NewGuid(), new UpdateUserProfileDto()))
            .Should().ThrowAsync<Exception>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── GetBookingHistoryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetBookingHistoryAsync_ReturnsPagedHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "Grand", Address = "A", City = "C", ContactNumber = "123", CreatedAt = DateTime.UtcNow };
        var reservations = new List<Reservation>
        {
            new() { ReservationId = Guid.NewGuid(), ReservationCode = "RES001", UserId = userId, HotelId = hotel.HotelId, Hotel = hotel, TotalAmount = 1000, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-3)), CreatedDate = DateTime.UtcNow.AddDays(-10) },
            new() { ReservationId = Guid.NewGuid(), ReservationCode = "RES002", UserId = userId, HotelId = hotel.HotelId, Hotel = hotel, TotalAmount = 500, Status = ReservationStatus.Cancelled, CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-2)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), CreatedDate = DateTime.UtcNow.AddDays(-5) }
        }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act
        var result = await _sut.GetBookingHistoryAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Bookings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBookingHistoryAsync_NoBookings_ReturnsEmpty()
    {
        // Arrange
        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act
        var result = await _sut.GetBookingHistoryAsync(Guid.NewGuid(), 1, 10);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Bookings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBookingHistoryAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "Grand", Address = "A", City = "C", ContactNumber = "123", CreatedAt = DateTime.UtcNow };
        var reservations = Enumerable.Range(1, 15).Select(i => new Reservation
        {
            ReservationId = Guid.NewGuid(),
            ReservationCode = $"RES{i:D3}",
            UserId = userId,
            HotelId = hotel.HotelId,
            Hotel = hotel,
            TotalAmount = 1000,
            Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-i)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-i + 1)),
            CreatedDate = DateTime.UtcNow.AddDays(-i)
        }).ToList().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act
        var result = await _sut.GetBookingHistoryAsync(userId, 2, 10);

        // Assert
        result.TotalCount.Should().Be(15);
        result.Bookings.Should().HaveCount(5); // page 2 of 10 = 5 remaining
    }
}
