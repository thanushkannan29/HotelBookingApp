using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Services;
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

    private ReviewService CreateSut() => new(
        _reviewRepoMock.Object, _hotelRepoMock.Object,
        _reservationRepoMock.Object, _userRepoMock.Object,
        _walletServiceMock.Object, _unitOfWorkMock.Object);

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    private static Review MakeReview(Guid userId, Guid hotelId, Guid reservationId) => new()
    {
        ReviewId = Guid.NewGuid(), UserId = userId, HotelId = hotelId,
        ReservationId = reservationId, Rating = 4m, Comment = "Good",
        CreatedDate = DateTime.UtcNow
    };

    private static Reservation MakeReservation(Guid reservationId, Guid userId, Guid hotelId) => new()
    {
        ReservationId = reservationId, UserId = userId, HotelId = hotelId,
        ReservationCode = "RES001", Status = ReservationStatus.Completed,
        CheckInDate = DateOnly.FromDateTime(DateTime.Today),
        CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        TotalAmount = 1000m, CreatedDate = DateTime.UtcNow
    };

    [Fact]
    public async Task AddReviewAsync_ValidInput_ReturnsDto()
    {
        var userId = Guid.NewGuid(); var hotelId = Guid.NewGuid(); var reservationId = Guid.NewGuid();
        var dto = new CreateReviewDto { HotelId = hotelId, ReservationId = reservationId, Rating = 5m, Comment = "Excellent" };
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Hotel> { new() { HotelId = hotelId, Name = "H", Address = "A", City = "C", ContactNumber = "9", CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { MakeReservation(reservationId, userId, hotelId) }.AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.AddAsync(It.IsAny<Review>())).ReturnsAsync((Review rv) => rv);
        var result = await CreateSut().AddReviewAsync(userId, dto);
        result.Rating.Should().Be(5m);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_HotelNotFound_ThrowsNotFoundException()
    {
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Hotel>().AsQueryable().BuildMock());
        var act = async () => await CreateSut().AddReviewAsync(Guid.NewGuid(), new CreateReviewDto { HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 4m, Comment = "X" });
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_ReservationNotCompleted_ThrowsReviewException()
    {
        var hotelId = Guid.NewGuid();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Hotel> { new() { HotelId = hotelId, Name = "H", Address = "A", City = "C", ContactNumber = "9", CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var act = async () => await CreateSut().AddReviewAsync(Guid.NewGuid(), new CreateReviewDto { HotelId = hotelId, ReservationId = Guid.NewGuid(), Rating = 4m, Comment = "X" });
        await act.Should().ThrowAsync<ReviewException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_AlreadyReviewed_ThrowsReviewException()
    {
        var userId = Guid.NewGuid(); var hotelId = Guid.NewGuid(); var reservationId = Guid.NewGuid();
        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Hotel> { new() { HotelId = hotelId, Name = "H", Address = "A", City = "C", ContactNumber = "9", CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { MakeReservation(reservationId, userId, hotelId) }.AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review> { MakeReview(userId, hotelId, reservationId) }.AsQueryable().BuildMock());
        var act = async () => await CreateSut().AddReviewAsync(userId, new CreateReviewDto { HotelId = hotelId, ReservationId = reservationId, Rating = 4m, Comment = "X" });
        await act.Should().ThrowAsync<ReviewException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReviewAsync_Owner_UpdatesAndReturnsDto()
    {
        var userId = Guid.NewGuid(); var hotelId = Guid.NewGuid(); var reservationId = Guid.NewGuid();
        var review = MakeReview(userId, hotelId, reservationId);
        review.Reservation = MakeReservation(reservationId, userId, hotelId);
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review> { review }.AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.UpdateAsync(review.ReviewId, review)).ReturnsAsync(review);
        var result = await CreateSut().UpdateReviewAsync(userId, review.ReviewId, new UpdateReviewDto { Rating = 3m, Comment = "Updated" });
        result.Rating.Should().Be(3m);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReviewAsync_NotOwner_ThrowsReviewException()
    {
        var ownerId = Guid.NewGuid(); var review = MakeReview(ownerId, Guid.NewGuid(), Guid.NewGuid());
        review.Reservation = MakeReservation(review.ReservationId, ownerId, review.HotelId);
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review> { review }.AsQueryable().BuildMock());
        var act = async () => await CreateSut().UpdateReviewAsync(Guid.NewGuid(), review.ReviewId, new UpdateReviewDto { Rating = 2m });
        await act.Should().ThrowAsync<ReviewException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReviewAsync_ReviewNotFound_ThrowsNotFoundException()
    {
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        var act = async () => await CreateSut().UpdateReviewAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateReviewDto { Rating = 3m });
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReviewAsync_NullImageUrl_DoesNotOverwrite()
    {
        var userId = Guid.NewGuid(); var review = MakeReview(userId, Guid.NewGuid(), Guid.NewGuid());
        review.ImageUrl = "original.jpg";
        review.Reservation = MakeReservation(review.ReservationId, userId, review.HotelId);
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review> { review }.AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.UpdateAsync(review.ReviewId, review)).ReturnsAsync(review);
        await CreateSut().UpdateReviewAsync(userId, review.ReviewId, new UpdateReviewDto { Rating = 3m, Comment = null, ImageUrl = null });
        review.ImageUrl.Should().Be("original.jpg");
    }

    [Fact]
    public async Task DeleteReviewAsync_Owner_ReturnsTrue()
    {
        var userId = Guid.NewGuid(); var review = MakeReview(userId, Guid.NewGuid(), Guid.NewGuid());
        _reviewRepoMock.Setup(r => r.GetAsync(review.ReviewId)).ReturnsAsync(review);
        _reviewRepoMock.Setup(r => r.DeleteAsync(review.ReviewId)).ReturnsAsync(review);
        var result = await CreateSut().DeleteReviewAsync(userId, review.ReviewId);
        result.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteReviewAsync_NotFound_ThrowsNotFoundException()
    {
        _reviewRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Review?)null);
        var act = async () => await CreateSut().DeleteReviewAsync(Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteReviewAsync_NotOwner_ThrowsReviewException()
    {
        var review = MakeReview(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _reviewRepoMock.Setup(r => r.GetAsync(review.ReviewId)).ReturnsAsync(review);
        var act = async () => await CreateSut().DeleteReviewAsync(Guid.NewGuid(), review.ReviewId);
        await act.Should().ThrowAsync<ReviewException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task GetReviewsByHotelAsync_ReturnsPagedResult()
    {
        var hotelId = Guid.NewGuid(); var userId = Guid.NewGuid(); var reservationId = Guid.NewGuid();
        var review = MakeReview(userId, hotelId, reservationId);
        review.Reservation = MakeReservation(reservationId, userId, hotelId);
        review.User = new User { UserId = userId, Name = "Alice", Email = "a@b.com", Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 }, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow };
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review> { review }.AsQueryable().BuildMock());
        var result = await CreateSut().GetReviewsByHotelAsync(hotelId, 1, 10);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminHotelReviewsAsync_ValidAdmin_ReturnsPagedResult()
    {
        var admin = MakeAdmin();
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        var result = await CreateSut().GetAdminHotelReviewsAsync(admin.UserId, 1, 10);
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAdminHotelReviewsAsync_WithAllSortDirs_Succeeds()
    {
        var admin = MakeAdmin();
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        var sut = CreateSut();
        (await sut.GetAdminHotelReviewsAsync(admin.UserId, 1, 10, minRating: 3, maxRating: 5, sortDir: "asc")).TotalCount.Should().Be(0);
        (await sut.GetAdminHotelReviewsAsync(admin.UserId, 1, 10, sortDir: "desc")).TotalCount.Should().Be(0);
        (await sut.GetAdminHotelReviewsAsync(admin.UserId, 1, 10)).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAdminHotelReviewsAsync_AdminNotFound_ThrowsUnAuthorizedException()
    {
        _userRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var act = async () => await CreateSut().GetAdminHotelReviewsAsync(Guid.NewGuid(), 1, 10);
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetAdminHotelReviewsAsync_AdminNoHotel_ThrowsUnAuthorizedException()
    {
        var admin = new User { UserId = Guid.NewGuid(), Name = "A", Email = "a@b.com", Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 }, Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        var act = async () => await CreateSut().GetAdminHotelReviewsAsync(admin.UserId, 1, 10);
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetMyReviewsPagedAsync_ReturnsUserReviews()
    {
        var userId = Guid.NewGuid(); var review = MakeReview(userId, Guid.NewGuid(), Guid.NewGuid());
        review.Hotel = new Hotel { HotelId = review.HotelId, Name = "Grand", Address = "A", City = "C", ContactNumber = "9", CreatedAt = DateTime.UtcNow };
        review.Reservation = MakeReservation(review.ReservationId, userId, review.HotelId);
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review> { review }.AsQueryable().BuildMock());
        var result = await CreateSut().GetMyReviewsPagedAsync(userId, 1, 10);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ReplyToReviewAsync_ValidAdmin_SetsReply()
    {
        var admin = MakeAdmin();
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        var review = MakeReview(Guid.NewGuid(), admin.HotelId!.Value, Guid.NewGuid());
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review> { review }.AsQueryable().BuildMock());
        await CreateSut().ReplyToReviewAsync(admin.UserId, review.ReviewId, "Thank you!");
        review.AdminReply.Should().Be("Thank you!");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReplyToReviewAsync_ReviewNotFound_ThrowsNotFoundException()
    {
        var admin = MakeAdmin();
        _userRepoMock.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        var act = async () => await CreateSut().ReplyToReviewAsync(admin.UserId, Guid.NewGuid(), "Reply");
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
