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
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
    private readonly Mock<IRepository<Guid, Review>> _reviewRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private UserService CreateSut() => new(
        _userRepoMock.Object, _reservationRepoMock.Object,
        _reviewRepoMock.Object, _unitOfWorkMock.Object);

    private static User MakeUserWithProfile(Guid? userId = null)
    {
        var id = userId ?? Guid.NewGuid();
        return new User
        {
            UserId = id, Name = "Alice", Email = "alice@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow,
            UserDetails = new UserProfileDetails
            {
                UserDetailsId = Guid.NewGuid(), UserId = id, Name = "Alice",
                Email = "alice@test.com", PhoneNumber = "9999999999",
                Address = "123 Main", State = "MH", City = "Mumbai",
                Pincode = "400001", CreatedAt = DateTime.UtcNow
            }
        };
    }

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
    {
        // Arrange
        var user = MakeUserWithProfile();
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
        var reviews = new List<Review>().AsQueryable().BuildMock();
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(reviews);
        var sut = CreateSut();

        // Act
        var result = await sut.GetProfileAsync(user.UserId);

        // Assert
        result.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var emptyUsers = new List<User>().AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(emptyUsers);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetProfileAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidDto_UpdatesAndReturns()
    {
        // Arrange
        var user = MakeUserWithProfile();
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
        var sut = CreateSut();
        var dto = new UpdateUserProfileDto { Name = "Alice Updated", PhoneNumber = "8888888888" };

        // Act
        var result = await sut.UpdateProfileAsync(user.UserId, dto);

        // Assert
        result.Name.Should().Be("Alice Updated");
    }

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var emptyUsers = new List<User>().AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(emptyUsers);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.UpdateProfileAsync(Guid.NewGuid(), new UpdateUserProfileDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_ReviewCount_TotalReviewPointsIs10PerReview()
    {
        // Arrange — user has 4 reviews → should be 4 × 10 = 40 pts (not 400)
        var user = MakeUserWithProfile();
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);

        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), UserId = user.UserId, HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 5, Comment = "Great!", CreatedDate = DateTime.UtcNow },
            new() { ReviewId = Guid.NewGuid(), UserId = user.UserId, HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Good!", CreatedDate = DateTime.UtcNow },
            new() { ReviewId = Guid.NewGuid(), UserId = user.UserId, HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 3, Comment = "Ok!", CreatedDate = DateTime.UtcNow },
            new() { ReviewId = Guid.NewGuid(), UserId = user.UserId, HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 5, Comment = "Loved it!", CreatedDate = DateTime.UtcNow },
        }.AsQueryable().BuildMock();
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(reviews);

        var sut = CreateSut();

        // Act
        var result = await sut.GetProfileAsync(user.UserId);

        // Assert — 4 reviews × 10 pts = 40, NOT 400
        result.TotalReviewPoints.Should().Be(40);
    }

    [Fact]
    public async Task GetBookingHistoryAsync_ValidUser_ReturnsPaged()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow };
        var reservations = new List<Reservation>
        {
            new() { ReservationId = Guid.NewGuid(), ReservationCode = "R1", UserId = userId, HotelId = hotel.HotelId, Hotel = hotel, TotalAmount = 1000, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        var sut = CreateSut();

        // Act
        var result = await sut.GetBookingHistoryAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }
}
