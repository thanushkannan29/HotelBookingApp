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
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepo = new();
    private readonly Mock<IRepository<Guid, Transaction>> _transactionRepo = new();
    private readonly Mock<IRepository<Guid, Review>> _reviewRepo = new();
    private readonly Mock<IRepository<Guid, Room>> _roomRepo = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepo = new();
    private readonly Mock<IRepository<Guid, RefundRequest>> _refundRepo = new();
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _sut = new DashboardService(
            _userRepo.Object, _hotelRepo.Object, _reservationRepo.Object,
            _transactionRepo.Object, _reviewRepo.Object, _roomRepo.Object,
            _roomTypeRepo.Object, _refundRepo.Object);
    }

    private static Hotel MakeHotel(Guid id) => new()
    {
        HotelId = id, Name = "Grand", Address = "A", City = "C",
        ContactNumber = "123", IsActive = true, CreatedAt = DateTime.UtcNow
    };

    // ── GetAdminDashboardAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminDashboardAsync_ValidAdmin_ReturnsDashboard()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        _hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(MakeHotel(hotelId));

        var rooms = new List<Room>
        {
            new() { RoomId = Guid.NewGuid(), RoomNumber = "101", Floor = 1, HotelId = hotelId, RoomTypeId = Guid.NewGuid(), IsActive = true },
            new() { RoomId = Guid.NewGuid(), RoomNumber = "102", Floor = 1, HotelId = hotelId, RoomTypeId = Guid.NewGuid(), IsActive = false }
        }.AsQueryable().BuildMock();
        _roomRepo.Setup(r => r.GetQueryable()).Returns(rooms);

        var roomTypes = new List<RoomType>
        {
            new() { RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Deluxe", IsActive = true }
        }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        var reservations = new List<Reservation>
        {
            new() { ReservationId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationCode = "R1", TotalAmount = 1000, Status = ReservationStatus.Confirmed, CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3)), CreatedDate = DateTime.UtcNow },
            new() { ReservationId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationCode = "R2", TotalAmount = 500, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-3)), CreatedDate = DateTime.UtcNow.AddDays(-10) }
        }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>
        {
            new() { TransactionId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Amount = 1500, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = new Reservation { HotelId = hotelId, UserId = Guid.NewGuid(), ReservationCode = "R", TotalAmount = 1500, CheckInDate = DateOnly.FromDateTime(DateTime.Now), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CreatedDate = DateTime.UtcNow } }
        }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Good", CreatedDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _reviewRepo.Setup(r => r.GetQueryable()).Returns(reviews);

        var refunds = new List<RefundRequest>().AsQueryable().BuildMock();
        _refundRepo.Setup(r => r.GetQueryable()).Returns(refunds);

        // Act
        var result = await _sut.GetAdminDashboardAsync(adminId);

        // Assert
        result.HotelId.Should().Be(hotelId);
        result.HotelName.Should().Be("Grand");
        result.TotalRooms.Should().Be(2);
        result.ActiveRooms.Should().Be(1);
        result.TotalRoomTypes.Should().Be(1);
        result.TotalReservations.Should().Be(2);
        result.TotalReviews.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminDashboardAsync_AdminNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        await _sut.Invoking(s => s.GetAdminDashboardAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>().WithMessage("*hotel*");
    }

    [Fact]
    public async Task GetAdminDashboardAsync_AdminNoHotel_ThrowsNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = null, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        await _sut.Invoking(s => s.GetAdminDashboardAsync(adminId))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── GetGuestDashboardAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetGuestDashboardAsync_ValidGuest_ReturnsDashboard()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservations = new List<Reservation>
        {
            new() { ReservationId = Guid.NewGuid(), UserId = userId, HotelId = Guid.NewGuid(), ReservationCode = "R1", TotalAmount = 1000, Status = ReservationStatus.Confirmed, CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3)), CreatedDate = DateTime.UtcNow },
            new() { ReservationId = Guid.NewGuid(), UserId = userId, HotelId = Guid.NewGuid(), ReservationCode = "R2", TotalAmount = 500, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-3)), CreatedDate = DateTime.UtcNow.AddDays(-10) },
            new() { ReservationId = Guid.NewGuid(), UserId = userId, HotelId = Guid.NewGuid(), ReservationCode = "R3", TotalAmount = 300, Status = ReservationStatus.Cancelled, CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-2)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), CreatedDate = DateTime.UtcNow.AddDays(-5) }
        }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>
        {
            new() { TransactionId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Amount = 1500, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = new Reservation { UserId = userId, HotelId = Guid.NewGuid(), ReservationCode = "R", TotalAmount = 1500, CheckInDate = DateOnly.FromDateTime(DateTime.Now), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CreatedDate = DateTime.UtcNow } }
        }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetGuestDashboardAsync(userId);

        // Assert
        result.TotalBookings.Should().Be(3);
        result.ActiveBookings.Should().Be(1);
        result.CompletedBookings.Should().Be(1);
        result.CancelledBookings.Should().Be(1);
        result.TotalSpent.Should().Be(1500m);
    }

    [Fact]
    public async Task GetGuestDashboardAsync_NoBookings_ReturnsZeros()
    {
        // Arrange
        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetGuestDashboardAsync(Guid.NewGuid());

        // Assert
        result.TotalBookings.Should().Be(0);
        result.TotalSpent.Should().Be(0m);
    }

    // ── GetSuperAdminDashboardAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetSuperAdminDashboardAsync_ReturnsSummary()
    {
        // Arrange
        var hotels = new List<Hotel>
        {
            new() { HotelId = Guid.NewGuid(), Name = "H1", Address = "A", City = "C", ContactNumber = "1", IsActive = true, IsBlockedBySuperAdmin = false, CreatedAt = DateTime.UtcNow },
            new() { HotelId = Guid.NewGuid(), Name = "H2", Address = "A", City = "C", ContactNumber = "2", IsActive = false, IsBlockedBySuperAdmin = true, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var users = new List<User>
        {
            new() { UserId = Guid.NewGuid(), Name = "U1", Email = "u1@test.com", CreatedAt = DateTime.UtcNow },
            new() { UserId = Guid.NewGuid(), Name = "U2", Email = "u2@test.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var reservations = new List<Reservation>
        {
            new() { ReservationId = Guid.NewGuid(), UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), ReservationCode = "R1", TotalAmount = 1000, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Now), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CreatedDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>
        {
            new() { TransactionId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Amount = 2000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), HotelId = Guid.NewGuid(), UserId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 5, Comment = "Great", CreatedDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _reviewRepo.Setup(r => r.GetQueryable()).Returns(reviews);

        // Act
        var result = await _sut.GetSuperAdminDashboardAsync();

        // Assert
        result.TotalHotels.Should().Be(2);
        result.ActiveHotels.Should().Be(1);
        result.BlockedHotels.Should().Be(1);
        result.TotalUsers.Should().Be(2);
        result.TotalReservations.Should().Be(1);
        result.TotalRevenue.Should().Be(2000m);
        result.TotalReviews.Should().Be(1);
    }

    [Fact]
    public async Task GetSuperAdminDashboardAsync_EmptyData_ReturnsZeros()
    {
        // Arrange
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(new List<Hotel>().AsQueryable().BuildMock());
        _userRepo.Setup(r => r.GetQueryable()).Returns(new List<User>().AsQueryable().BuildMock());
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        _reviewRepo.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());

        // Act
        var result = await _sut.GetSuperAdminDashboardAsync();

        // Assert
        result.TotalHotels.Should().Be(0);
        result.TotalRevenue.Should().Be(0m);
    }
}
