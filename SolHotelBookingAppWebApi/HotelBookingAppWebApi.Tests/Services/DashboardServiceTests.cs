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
    private readonly Mock<IRepository<Guid, SuperAdminRevenue>> _revenueRepoMock = new();

    private DashboardService CreateSut() => new(
        _userRepoMock.Object, _hotelRepoMock.Object, _reservationRepoMock.Object,
        _transactionRepoMock.Object, _reviewRepoMock.Object,
        _roomRepoMock.Object, _roomTypeRepoMock.Object, _revenueRepoMock.Object);

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
    public async Task GetAdminDashboardAsync_TotalRevenue_OnlyCountsCompletedReservations()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var completedResId = Guid.NewGuid();
        var confirmedResId = Guid.NewGuid();

        var users = new List<User> { new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
        _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(MakeHotel(hotelId));
        _roomRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Room>().AsQueryable().BuildMock());
        _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RoomType>().AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());

        // Two transactions: one for a Completed reservation (should count), one for Confirmed (should NOT)
        var transactions = new List<Transaction>
        {
            new() { TransactionId = Guid.NewGuid(), ReservationId = completedResId, Amount = 1000m, Status = PaymentStatus.Success, PaymentMethod = PaymentMethod.UPI, TransactionDate = DateTime.UtcNow,
                Reservation = new Reservation { ReservationId = completedResId, HotelId = hotelId, Status = ReservationStatus.Completed, ReservationCode = "R1", UserId = Guid.NewGuid(), CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow } },
            new() { TransactionId = Guid.NewGuid(), ReservationId = confirmedResId, Amount = 500m, Status = PaymentStatus.Success, PaymentMethod = PaymentMethod.UPI, TransactionDate = DateTime.UtcNow,
                Reservation = new Reservation { ReservationId = confirmedResId, HotelId = hotelId, Status = ReservationStatus.Confirmed, ReservationCode = "R2", UserId = Guid.NewGuid(), CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow } },
        }.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAdminDashboardAsync(adminId);

        // Assert — only the Completed reservation's ₹1000 counts, NOT the Confirmed ₹500
        result.TotalRevenue.Should().Be(1000m);
    }

    [Fact]
    public async Task GetGuestDashboardAsync_TotalSpent_OnlyCountsCompletedReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        var reservations = new List<Reservation>
        {
            // Completed — should count
            new() { ReservationId = Guid.NewGuid(), UserId = guestId, HotelId = hotelId, Status = ReservationStatus.Completed, FinalAmount = 1200m, ReservationCode = "R1", CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow },
            // Cancelled — should NOT count (refunded)
            new() { ReservationId = Guid.NewGuid(), UserId = guestId, HotelId = hotelId, Status = ReservationStatus.Cancelled, FinalAmount = 800m, ReservationCode = "R2", CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow },
            // Confirmed — should NOT count (not yet completed)
            new() { ReservationId = Guid.NewGuid(), UserId = guestId, HotelId = hotelId, Status = ReservationStatus.Confirmed, FinalAmount = 600m, ReservationCode = "R3", CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow },
        }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetGuestDashboardAsync(guestId);

        // Assert — only the Completed ₹1200 counts, not Cancelled ₹800 or Confirmed ₹600
        result.TotalSpent.Should().Be(1200m);
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
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(new List<SuperAdminRevenue>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetSuperAdminDashboardAsync();

        // Assert
        result.TotalHotels.Should().Be(1);
        result.TotalRevenue.Should().Be(0);
    }

    [Fact]
    public async Task GetSuperAdminDashboardAsync_TotalRevenue_IsCommissionNotFullBookingAmount()
    {
        // Arrange — 2 completed reservations, each with ₹1000 booking → 2% = ₹20 each → total ₹40
        var res1 = Guid.NewGuid();
        var res2 = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Hotel>().AsQueryable().BuildMock());
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().AsQueryable().BuildMock());
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());

        var commissions = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = res1, HotelId = hotelId, ReservationAmount = 1000m, CommissionAmount = 20m, SuperAdminUpiId = "test@upi", CreatedAt = DateTime.UtcNow },
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = res2, HotelId = hotelId, ReservationAmount = 1000m, CommissionAmount = 20m, SuperAdminUpiId = "test@upi", CreatedAt = DateTime.UtcNow },
        }.AsQueryable().BuildMock();
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(commissions);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSuperAdminDashboardAsync();

        // Assert — platform revenue = ₹40 commission, NOT ₹2000 total booking amount
        result.TotalRevenue.Should().Be(40m);
    }
}
