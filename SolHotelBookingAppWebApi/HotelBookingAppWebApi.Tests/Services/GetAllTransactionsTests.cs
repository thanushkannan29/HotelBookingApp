using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

/// <summary>
/// Additional tests for TransactionService.GetAllTransactionsAsync covering
/// Guest, Admin, and SuperAdmin role branches for 100% coverage.
/// </summary>
public class GetAllTransactionsTests
{
    private readonly Mock<IRepository<Guid, Transaction>> _transactionRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepo = new();
    private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepo = new();
    private readonly Mock<IRepository<Guid, ReservationRoom>> _reservationRoomRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IRepository<Guid, Wallet>> _walletRepo = new();
    private readonly Mock<IRepository<Guid, WalletTransaction>> _walletTxRepo = new();
    private readonly Mock<IRepository<Guid, SuperAdminRevenue>> _revenueRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly TransactionService _sut;

    public GetAllTransactionsTests()
    {
        _sut = new TransactionService(
            _transactionRepo.Object, _reservationRepo.Object, _inventoryRepo.Object,
            _reservationRoomRepo.Object, _userRepo.Object, _hotelRepo.Object,
            _walletRepo.Object, _walletTxRepo.Object, _revenueRepo.Object, _unitOfWork.Object);
    }

    private static Transaction MakeTx(Guid userId, Guid hotelId) => new()
    {
        TransactionId = Guid.NewGuid(),
        ReservationId = Guid.NewGuid(),
        Amount = 1000m,
        PaymentMethod = PaymentMethod.UPI,
        Status = PaymentStatus.Success,
        TransactionDate = DateTime.UtcNow,
        Reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            ReservationCode = "RES001",
            UserId = userId,
            HotelId = hotelId,
            TotalAmount = 1000m,
            Status = ReservationStatus.Confirmed,
            CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
            CreatedDate = DateTime.UtcNow,
            Hotel = new Hotel { HotelId = hotelId, Name = "Grand", Address = "A", City = "C", ContactNumber = "1", CreatedAt = DateTime.UtcNow },
            User = new User { UserId = userId, Name = "Guest", Email = "g@g.com", CreatedAt = DateTime.UtcNow }
        }
    };

    // ── Guest role ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllTransactionsAsync_GuestRole_ReturnsGuestTransactions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var tx = MakeTx(userId, hotelId);
        var transactions = new List<Transaction> { tx }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // No wallet refunds
        var wallets = new List<Wallet>().AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        // Act
        var result = await _sut.GetAllTransactionsAsync(userId, "Guest", 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Transactions.First().GuestName.Should().Be("Guest");
    }

    [Fact]
    public async Task GetAllTransactionsAsync_GuestRole_WithWalletRefunds_IncludesRefunds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var tx = MakeTx(userId, hotelId);
        var transactions = new List<Transaction> { tx }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        var walletId = Guid.NewGuid();
        var wallet = new Wallet { WalletId = walletId, UserId = userId, Balance = 500m, UpdatedAt = DateTime.UtcNow };
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        var walletTxs = new List<WalletTransaction>
        {
            new() { WalletTransactionId = Guid.NewGuid(), WalletId = walletId, Amount = 200m, Type = "Credit", Description = "Refund for reservation RES001", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _walletTxRepo.Setup(r => r.GetQueryable()).Returns(walletTxs);

        // Act
        var result = await _sut.GetAllTransactionsAsync(userId, "Guest", 1, 10);

        // Assert
        result.TotalCount.Should().Be(2); // 1 payment + 1 wallet refund
        result.Transactions.Should().Contain(t => t.TransactionType == "WalletRefund");
    }

    // ── Admin role ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllTransactionsAsync_AdminRole_ReturnsHotelTransactions()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var tx = MakeTx(Guid.NewGuid(), hotelId);
        var transactions = new List<Transaction> { tx }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // No commissions or auto-refunds
        var revenues = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(revenues);

        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var wallets = new List<Wallet>().AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        var walletTxs = new List<WalletTransaction>().AsQueryable().BuildMock();
        _walletTxRepo.Setup(r => r.GetQueryable()).Returns(walletTxs);

        // Act
        var result = await _sut.GetAllTransactionsAsync(adminId, "Admin", 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_AdminRole_AdminHotelNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // User has no HotelId
        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = null, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        // Act & Assert
        await _sut.Invoking(s => s.GetAllTransactionsAsync(adminId, "Admin", 1, 10))
            .Should().ThrowAsync<NotFoundException>().WithMessage("*hotel*");
    }

    // ── SuperAdmin role ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllTransactionsAsync_SuperAdminRole_ReturnsAllTransactions()
    {
        // Arrange
        var tx1 = MakeTx(Guid.NewGuid(), Guid.NewGuid());
        var tx2 = MakeTx(Guid.NewGuid(), Guid.NewGuid());
        var transactions = new List<Transaction> { tx1, tx2 }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetAllTransactionsAsync(Guid.NewGuid(), "SuperAdmin", 1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_SuperAdminRole_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var transactions = Enumerable.Range(1, 15).Select(_ => MakeTx(Guid.NewGuid(), Guid.NewGuid())).ToList().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetAllTransactionsAsync(Guid.NewGuid(), "SuperAdmin", 2, 10);

        // Assert
        result.TotalCount.Should().Be(15);
        result.Transactions.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_AdminRole_WithCommissions_IncludesCommissions()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var revenues = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = reservationId, HotelId = hotelId, CommissionAmount = 20m, CreatedAt = DateTime.UtcNow,
                Reservation = new Reservation { ReservationId = reservationId, ReservationCode = "RES001", UserId = Guid.NewGuid(), HotelId = hotelId, TotalAmount = 1000m, CheckInDate = DateOnly.FromDateTime(DateTime.Now), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CreatedDate = DateTime.UtcNow } }
        }.AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(revenues);

        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var wallets = new List<Wallet>().AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        var walletTxs = new List<WalletTransaction>().AsQueryable().BuildMock();
        _walletTxRepo.Setup(r => r.GetQueryable()).Returns(walletTxs);

        // Act
        var result = await _sut.GetAllTransactionsAsync(adminId, "Admin", 1, 10);

        // Assert
        result.Transactions.Should().Contain(t => t.TransactionType == "CommissionSent");
    }
}
