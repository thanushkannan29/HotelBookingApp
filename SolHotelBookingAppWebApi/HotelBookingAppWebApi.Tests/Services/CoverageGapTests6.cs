using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

/// <summary>
/// Targets remaining uncovered lines in TransactionService:
/// - MarkTransactionFailedAsync with inventory restore (reservationRooms.Count > 0 branch)
/// - MarkTransactionFailedAsync admin not found / no hotel
/// - GetPaymentIntentAsync reservation not found
/// - RecordFailedPaymentAsync reservation not found
/// - GetAllTransactionsAsync admin with null hotelId (no extras appended)
/// - GetAllTransactionsAsync guest with no wallet (wallet is null branch)
/// - CreatePaymentAsync uses FinalAmount=0 → falls back to TotalAmount
/// </summary>
public class CoverageGapTests6
{
    private readonly Mock<IRepository<Guid, Transaction>> _txRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _resRepo = new();
    private readonly Mock<IRepository<Guid, RoomTypeInventory>> _invRepo = new();
    private readonly Mock<IRepository<Guid, ReservationRoom>> _rrRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IRepository<Guid, Wallet>> _walletRepo = new();
    private readonly Mock<IRepository<Guid, WalletTransaction>> _walletTxRepo = new();
    private readonly Mock<IRepository<Guid, SuperAdminRevenue>> _revenueRepo = new();
    private readonly Mock<IWalletService> _walletServiceMock = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private TransactionService CreateSut() => new(
        _txRepo.Object, _resRepo.Object, _invRepo.Object, _rrRepo.Object,
        _userRepo.Object, _hotelRepo.Object, _walletRepo.Object,
        _walletTxRepo.Object, _revenueRepo.Object, _walletServiceMock.Object, _uow.Object);

    private static Reservation MakeReservation(Guid userId, Guid hotelId,
        ReservationStatus status = ReservationStatus.Confirmed) => new()
    {
        ReservationId = Guid.NewGuid(), ReservationCode = "RES-GAP01",
        UserId = userId, HotelId = hotelId, Status = status,
        CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
        TotalAmount = 1000, FinalAmount = 1000,
        CreatedDate = DateTime.UtcNow,
        Transactions = new List<Transaction>()
    };

    // ── MarkTransactionFailedAsync: admin not found ───────────────────────────

    [Fact]
    public async Task MarkTransactionFailedAsync_AdminNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.MarkTransactionFailedAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>().WithMessage("*Unauthorized*");
    }

    // ── MarkTransactionFailedAsync: admin has no hotel ────────────────────────

    [Fact]
    public async Task MarkTransactionFailedAsync_AdminHasNoHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            UserId = adminId, HotelId = null, Name = "Admin",
            Email = "a@b.com", Password = new byte[] { 1 },
            PasswordSaltValue = new byte[] { 2 }, Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.MarkTransactionFailedAsync(Guid.NewGuid(), adminId);

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>().WithMessage("*Unauthorized*");
    }

    // ── MarkTransactionFailedAsync: transaction not found ────────────────────

    [Fact]
    public async Task MarkTransactionFailedAsync_TransactionNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = new User
        {
            UserId = adminId, HotelId = hotelId, Name = "Admin",
            Email = "a@b.com", Password = new byte[] { 1 },
            PasswordSaltValue = new byte[] { 2 }, Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.MarkTransactionFailedAsync(Guid.NewGuid(), adminId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Transaction not found*");
    }

    // ── MarkTransactionFailedAsync: with reservation rooms → restores inventory

    [Fact]
    public async Task MarkTransactionFailedAsync_WithReservationRooms_RestoresInventory()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = new User
        {
            UserId = adminId, HotelId = hotelId, Name = "Admin",
            Email = "a@b.com", Password = new byte[] { 1 },
            PasswordSaltValue = new byte[] { 2 }, Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var reservation = MakeReservation(Guid.NewGuid(), hotelId, ReservationStatus.Confirmed);
        var tx = new Transaction
        {
            TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId,
            Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow, Reservation = reservation
        };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());

        var rr = new ReservationRoom
        {
            ReservationRoomId = Guid.NewGuid(), ReservationId = reservation.ReservationId,
            RoomId = Guid.NewGuid(), RoomTypeId = roomTypeId
        };
        _rrRepo.Setup(r => r.GetQueryable()).Returns(new List<ReservationRoom> { rr }.AsQueryable().BuildMock());

        var inv = new RoomTypeInventory
        {
            RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId,
            Date = reservation.CheckInDate, TotalInventory = 5, ReservedInventory = 3
        };
        _invRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomTypeInventory> { inv }.AsQueryable().BuildMock());

        var sut = CreateSut();

        // Act
        await sut.MarkTransactionFailedAsync(tx.TransactionId, adminId);

        // Assert — inventory restored, transaction marked failed, reservation reset to Pending
        tx.Status.Should().Be(PaymentStatus.Failed);
        reservation.Status.Should().Be(ReservationStatus.Pending);
        inv.ReservedInventory.Should().Be(2); // 3 - 1 room
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── GetPaymentIntentAsync: reservation not found ──────────────────────────

    [Fact]
    public async Task GetPaymentIntentAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetPaymentIntentAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Reservation not found*");
    }

    // ── RecordFailedPaymentAsync: reservation not found ──────────────────────

    [Fact]
    public async Task RecordFailedPaymentAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.RecordFailedPaymentAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Reservation not found*");
    }

    // ── GetAllTransactionsAsync: guest with no wallet (wallet is null) ────────

    [Fact]
    public async Task GetAllTransactionsAsync_GuestRole_NoWallet_ReturnsOnlyPayments()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var reservation = MakeReservation(guestId, Guid.NewGuid());
        var tx = new Transaction
        {
            TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId,
            Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow, Reservation = reservation
        };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        // No wallet exists for this guest
        _walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllTransactionsAsync(guestId, "Guest", 1, 10);

        // Assert — only the payment, no wallet refunds appended
        result.TotalCount.Should().Be(1);
        result.Transactions.Should().HaveCount(1);
        result.Transactions.First().TransactionType.Should().Be("Payment");
    }

    // ── GetAllTransactionsAsync: admin with null hotelId (no extras) ──────────

    [Fact]
    public async Task GetAllTransactionsAsync_AdminRole_NullHotelId_ThrowsNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        // User has no HotelId → GetQueryable returns null for HotelId
        _userRepo.Setup(r => r.GetQueryable()).Returns(new List<User>
        {
            new() { UserId = adminId, HotelId = null, Name = "Admin", Email = "a@b.com",
                    Password = new byte[]{1}, PasswordSaltValue = new byte[]{2},
                    Role = UserRole.Admin, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock());
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetAllTransactionsAsync(adminId, "Admin", 1, 10);

        // Assert — throws because admin hotel not found
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Admin hotel not found*");
    }

    // ── CreatePaymentAsync: FinalAmount=0 uses TotalAmount ───────────────────

    [Fact]
    public async Task CreatePaymentAsync_FinalAmountZero_UsesTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Pending);
        reservation.FinalAmount = 0;
        reservation.TotalAmount = 1500;
        reservation.ExpiryTime = DateTime.UtcNow.AddHours(1);
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);
        var sut = CreateSut();

        // Act
        var result = await sut.CreatePaymentAsync(new CreatePaymentDto
        {
            ReservationId = reservation.ReservationId,
            PaymentMethod = PaymentMethod.UPI
        });

        // Assert — amount falls back to TotalAmount when FinalAmount is 0
        result.Amount.Should().Be(1500);
        result.Status.Should().Be(PaymentStatus.Success);
    }

    // ── DirectGuestRefundAsync: transaction not found ─────────────────────────

    [Fact]
    public async Task DirectGuestRefundAsync_TransactionNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.DirectGuestRefundAsync(
            Guid.NewGuid(), Guid.NewGuid(), new RefundRequestDto { Reason = "Test" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Transaction not found*");
    }

    // ── RecordFailedPaymentAsync: FinalAmount=0 uses TotalAmount ─────────────

    [Fact]
    public async Task RecordFailedPaymentAsync_FinalAmountZero_UsesTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Pending);
        reservation.FinalAmount = 0;
        reservation.TotalAmount = 2000;
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);
        var sut = CreateSut();

        // Act
        await sut.RecordFailedPaymentAsync(reservation.ReservationId, userId);

        // Assert — amount uses TotalAmount when FinalAmount is 0
        _txRepo.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Amount == 2000 && t.Status == PaymentStatus.Failed)), Times.Once);
    }
}
