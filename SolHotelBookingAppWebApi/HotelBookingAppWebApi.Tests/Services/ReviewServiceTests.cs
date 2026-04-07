using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Services;
using Microsoft.Extensions.Configuration;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class ReviewServiceTests
{
    private readonly Mock<IRepository<Guid, Review>> _reviewRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IWalletService> _walletServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    /// <summary>Builds a configuration with ReviewSettings:RewardPoints = 10.</summary>
    private static IConfiguration BuildConfig(decimal rewardPoints = 10m)
    {
        var inMemory = new Dictionary<string, string?> { ["ReviewSettings:RewardPoints"] = rewardPoints.ToString() };
        return new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
    }

    private ReviewService CreateSut(decimal rewardPoints = 10m) => new(
        _reviewRepoMock.Object, _hotelRepoMock.Object, _reservationRepoMock.Object,
        _userRepoMock.Object, _walletServiceMock.Object, _unitOfWorkMock.Object,
        BuildConfig(rewardPoints));

    private static User MakeAdmin(Guid hotelId) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId, CreatedAt = DateTime.UtcNow
    };


    // ── AddReviewAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AddReviewAsync_ValidDto_ReturnsReviewResponseDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand", Address = "Addr", City = "City", State = "State", ContactNumber = "123", UpiId = "upi@test", CreatedAt = DateTime.UtcNow };
        var reservation = new Reservation
        {
            ReservationId = reservationId, UserId = userId, HotelId = hotelId,
            ReservationCode = "RES001", Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalAmount = 1000, FinalAmount = 1000, CreatedDate = DateTime.UtcNow
        };

        _hotelRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Review>().AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.AddAsync(It.IsAny<Review>())).ReturnsAsync((Review rv) => rv);

        var sut = CreateSut();
        var dto = new CreateReviewDto { HotelId = hotelId, ReservationId = reservationId, Rating = 4, Comment = "Great stay!" };

        // Act
        var result = await sut.AddReviewAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be(4);
        result.Comment.Should().Be("Great stay!");
        _walletServiceMock.Verify(w => w.CreditAsync(userId, 10m, It.IsAny<string>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_HotelNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _hotelRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel>().AsQueryable().BuildMock());
        var sut = CreateSut();
        var dto = new CreateReviewDto { HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Nice" };

        // Act
        var act = async () => await sut.AddReviewAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_ReservationNotCompleted_ThrowsReviewException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand", Address = "Addr", City = "City", State = "State", ContactNumber = "123", UpiId = "upi@test", CreatedAt = DateTime.UtcNow };
        _hotelRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Reservation>().AsQueryable().BuildMock());
        var sut = CreateSut();
        var dto = new CreateReviewDto { HotelId = hotelId, ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Nice" };

        // Act
        var act = async () => await sut.AddReviewAsync(userId, dto);

        // Assert
        await act.Should().ThrowAsync<ReviewException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_AlreadyReviewed_ThrowsReviewException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand", Address = "Addr", City = "City", State = "State", ContactNumber = "123", UpiId = "upi@test", CreatedAt = DateTime.UtcNow };
        var reservation = new Reservation
        {
            ReservationId = reservationId, UserId = userId, HotelId = hotelId,
            ReservationCode = "RES001", Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalAmount = 1000, FinalAmount = 1000, CreatedDate = DateTime.UtcNow
        };
        var existingReview = new Review { ReviewId = Guid.NewGuid(), ReservationId = reservationId, HotelId = hotelId, UserId = userId, Rating = 5, Comment = "Old", CreatedDate = DateTime.UtcNow };

        _hotelRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Review> { existingReview }.AsQueryable().BuildMock());
        var sut = CreateSut();
        var dto = new CreateReviewDto { HotelId = hotelId, ReservationId = reservationId, Rating = 4, Comment = "Nice" };

        // Act
        var act = async () => await sut.AddReviewAsync(userId, dto);

        // Assert
        await act.Should().ThrowAsync<ReviewException>().WithMessage("*already submitted*");
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }


    // ── UpdateReviewAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateReviewAsync_ValidOwner_UpdatesAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES002", UserId = userId,
            HotelId = Guid.NewGuid(), Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalAmount = 1000, FinalAmount = 1000, CreatedDate = DateTime.UtcNow
        };
        var review = new Review
        {
            ReviewId = reviewId, UserId = userId, HotelId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId, Rating = 3, Comment = "Old comment",
            CreatedDate = DateTime.UtcNow, Reservation = reservation
        };
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Review> { review }.AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.UpdateAsync(reviewId, review)).ReturnsAsync(review);
        var sut = CreateSut();
        var dto = new UpdateReviewDto { Rating = 5, Comment = "Updated comment", ImageUrl = "img.jpg" };

        // Act
        var result = await sut.UpdateReviewAsync(userId, reviewId, dto);

        // Assert
        result.Rating.Should().Be(5);
        result.Comment.Should().Be("Updated comment");
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReviewAsync_ReviewNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Review>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.UpdateReviewAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateReviewDto { Rating = 4 });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReviewAsync_NotOwner_ThrowsReviewException()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var review = new Review
        {
            ReviewId = reviewId, UserId = ownerId, HotelId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(), Rating = 3, Comment = "Old",
            CreatedDate = DateTime.UtcNow
        };
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Review> { review }.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.UpdateReviewAsync(otherId, reviewId, new UpdateReviewDto { Rating = 4 });

        // Assert
        await act.Should().ThrowAsync<ReviewException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── DeleteReviewAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteReviewAsync_ValidOwner_DeletesAndReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var review = new Review
        {
            ReviewId = reviewId, UserId = userId, HotelId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Good",
            CreatedDate = DateTime.UtcNow
        };
        _reviewRepoMock.Setup(r => r.GetAsync(reviewId)).ReturnsAsync(review);
        _reviewRepoMock.Setup(r => r.DeleteAsync(reviewId)).ReturnsAsync(review);
        var sut = CreateSut();

        // Act
        var result = await sut.DeleteReviewAsync(userId, reviewId);

        // Assert
        result.Should().BeTrue();
        _walletServiceMock.Verify(w => w.DebitAsync(userId, 10m, It.IsAny<string>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteReviewAsync_ReviewNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _reviewRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Review?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.DeleteReviewAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteReviewAsync_NotOwner_ThrowsReviewException()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var review = new Review
        {
            ReviewId = reviewId, UserId = ownerId, HotelId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Good",
            CreatedDate = DateTime.UtcNow
        };
        _reviewRepoMock.Setup(r => r.GetAsync(reviewId)).ReturnsAsync(review);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.DeleteReviewAsync(Guid.NewGuid(), reviewId);

        // Assert
        await act.Should().ThrowAsync<ReviewException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }


    // ── GetReviewsByHotelAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetReviewsByHotelAsync_ReturnsPagedResult()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES003", UserId = Guid.NewGuid(),
            HotelId = hotelId, Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalAmount = 1000, FinalAmount = 1000, CreatedDate = DateTime.UtcNow
        };
        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Rating = 4, Comment = "Good", CreatedDate = DateTime.UtcNow, Reservation = reservation },
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Rating = 5, Comment = "Excellent", CreatedDate = DateTime.UtcNow, Reservation = reservation }
        };
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(reviews.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetReviewsByHotelAsync(hotelId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Reviews.Should().HaveCount(2);
    }

    // ── GetAdminHotelReviewsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAdminHotelReviewsAsync_WithFilters_ReturnsFilteredResult()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES004", UserId = admin.UserId,
            HotelId = hotelId, Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalAmount = 1000, FinalAmount = 1000, CreatedDate = DateTime.UtcNow
        };
        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Rating = 5, Comment = "Excellent", CreatedDate = DateTime.UtcNow, Reservation = reservation },
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Rating = 2, Comment = "Bad", CreatedDate = DateTime.UtcNow, Reservation = reservation }
        };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(reviews.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetAdminHotelReviewsAsync(admin.UserId, 1, 10, minRating: 4, maxRating: 5, sortDir: "asc");

        // Assert
        result.TotalCount.Should().Be(1);
        result.Reviews.First().Rating.Should().Be(5);
    }

    [Fact]
    public async Task GetAdminHotelReviewsAsync_SortDesc_ReturnsSortedResult()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES005", UserId = admin.UserId,
            HotelId = hotelId, Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalAmount = 1000, FinalAmount = 1000, CreatedDate = DateTime.UtcNow
        };
        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Rating = 3, Comment = "Ok", CreatedDate = DateTime.UtcNow.AddMinutes(-1), Reservation = reservation },
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Rating = 5, Comment = "Great", CreatedDate = DateTime.UtcNow, Reservation = reservation }
        };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(reviews.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetAdminHotelReviewsAsync(admin.UserId, 1, 10, sortDir: "desc");

        // Assert
        result.Reviews.First().Rating.Should().Be(5);
    }

    [Fact]
    public async Task GetAdminHotelReviewsAsync_AdminNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetAdminHotelReviewsAsync(Guid.NewGuid(), 1, 10);

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetAdminHotelReviewsAsync_AdminHasNoHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 }, Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetAdminHotelReviewsAsync(admin.UserId, 1, 10);

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetMyReviewsPagedAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyReviewsPagedAsync_ReturnsUserReviews()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "Grand", Address = "Addr", City = "City", State = "State", ContactNumber = "123", UpiId = "upi@test", CreatedAt = DateTime.UtcNow };
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES006", UserId = userId,
            HotelId = hotel.HotelId, Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TotalAmount = 1000, FinalAmount = 1000, CreatedDate = DateTime.UtcNow
        };
        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), HotelId = hotel.HotelId, UserId = userId, ReservationId = reservation.ReservationId, Rating = 4, Comment = "Nice", CreatedDate = DateTime.UtcNow, Hotel = hotel, Reservation = reservation }
        };
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(reviews.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetMyReviewsPagedAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Reviews.First().HotelName.Should().Be("Grand");
    }

    // ── ReplyToReviewAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ReplyToReviewAsync_ValidAdmin_SetsAdminReply()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var reviewId = Guid.NewGuid();
        var review = new Review
        {
            ReviewId = reviewId, HotelId = hotelId, UserId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Good",
            CreatedDate = DateTime.UtcNow
        };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Review> { review }.AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        await sut.ReplyToReviewAsync(admin.UserId, reviewId, "Thank you for your feedback!");

        // Assert
        review.AdminReply.Should().Be("Thank you for your feedback!");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReplyToReviewAsync_ReviewNotInHotel_ThrowsNotFoundException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _reviewRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Review>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.ReplyToReviewAsync(admin.UserId, Guid.NewGuid(), "Reply");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
