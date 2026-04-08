using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

/// <summary>
/// Targets specific uncovered lines identified from the cobertura coverage report.
/// </summary>
public class CoverageGapTests3
{
    private static HotelBookingContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HotelBookingContext(opts);
    }

    // ── TransactionService helpers ────────────────────────────────────────────

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

    private TransactionService CreateTxSut() => new(
        _txRepo.Object, _resRepo.Object, _invRepo.Object, _rrRepo.Object,
        _userRepo.Object, _hotelRepo.Object, _walletRepo.Object,
        _walletTxRepo.Object, _revenueRepo.Object, _walletServiceMock.Object, _uow.Object);

    private static Reservation MakeReservation(Guid userId, Guid hotelId,
        ReservationStatus status = ReservationStatus.Confirmed) => new()
    {
        ReservationId = Guid.NewGuid(), ReservationCode = "RES-TEST01",
        UserId = userId, HotelId = hotelId, Status = status,
        CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
        TotalAmount = 1000, FinalAmount = 1000,
        CreatedDate = DateTime.UtcNow,
        Transactions = new List<Transaction>()
    };

    // ── TransactionService: GetAllTransactionsAsync — Admin role ──────────────

    [Fact]
    public async Task GetAllTransactionsAsync_AdminRole_ReturnsAdminTransactions()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservation = MakeReservation(Guid.NewGuid(), hotelId);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        _userRepo.Setup(r => r.GetQueryable()).Returns(new List<User> { new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock());
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet>().AsQueryable().BuildMock());
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(new List<SuperAdminRevenue>().AsQueryable().BuildMock());
        _walletTxRepo.Setup(r => r.GetQueryable()).Returns(new List<WalletTransaction>().AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var result = await sut.GetAllTransactionsAsync(adminId, "Admin", 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_SuperAdminRole_ReturnsAllTransactions()
    {
        // Arrange
        var superAdminId = Guid.NewGuid();
        var reservation = MakeReservation(Guid.NewGuid(), Guid.NewGuid());
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 2000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        _walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet>().AsQueryable().BuildMock());
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(new List<SuperAdminRevenue>().AsQueryable().BuildMock());
        _walletTxRepo.Setup(r => r.GetQueryable()).Returns(new List<WalletTransaction>().AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var result = await sut.GetAllTransactionsAsync(superAdminId, "SuperAdmin", 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_GuestRole_IncludesWalletRefunds()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var reservation = MakeReservation(guestId, Guid.NewGuid());
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        var walletTx = new WalletTransaction { WalletTransactionId = Guid.NewGuid(), WalletId = walletId, Amount = 200, Type = "Credit", Description = "Refund", CreatedAt = DateTime.UtcNow };
        var wallet = new Wallet { WalletId = walletId, UserId = guestId, Balance = 200, UpdatedAt = DateTime.UtcNow };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        _walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        _walletTxRepo.Setup(r => r.GetQueryable()).Returns(new List<WalletTransaction> { walletTx }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var result = await sut.GetAllTransactionsAsync(guestId, "Guest", 1, 10);

        // Assert — 1 payment + 1 wallet refund
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllTransactionsAsync_AdminRole_IncludesCommissionsAndAutoRefunds()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var reservation = MakeReservation(guestId, hotelId);
        reservation.ReservationId = reservationId;
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        var commission = new SuperAdminRevenue { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = reservationId, HotelId = hotelId, ReservationAmount = 1000, CommissionAmount = 20, SuperAdminUpiId = "sa@upi", CreatedAt = DateTime.UtcNow, Reservation = reservation };
        var wallet = new Wallet { WalletId = walletId, UserId = guestId, Balance = 0, UpdatedAt = DateTime.UtcNow };
        var autoRefundTx = new WalletTransaction { WalletTransactionId = Guid.NewGuid(), WalletId = walletId, Amount = 100, Type = "Credit", Description = "Refund for cancelled reservation RES-TEST01 (hotel deactivated)", CreatedAt = DateTime.UtcNow };
        var adminUser = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow };
        var guestUser = new User { UserId = guestId, HotelId = null, Name = "Guest", Email = "g@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow };
        // _userRepo.GetQueryable() is called twice: once for admin hotel ID, once for guest names
        _userRepo.Setup(r => r.GetQueryable()).Returns(new List<User> { adminUser, guestUser }.AsQueryable().BuildMock());
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(new List<SuperAdminRevenue> { commission }.AsQueryable().BuildMock());
        _walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        _walletTxRepo.Setup(r => r.GetQueryable()).Returns(new List<WalletTransaction> { autoRefundTx }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var result = await sut.GetAllTransactionsAsync(adminId, "Admin", 1, 10);

        // Assert — 1 payment + 1 commission + 1 auto refund
        result.TotalCount.Should().Be(3);
    }

    // ── TransactionService: ValidateReservationForPayment branches ───────────

    [Fact]
    public async Task CreatePaymentAsync_CompletedReservation_ThrowsPaymentException()
    {
        // Arrange
        var reservation = MakeReservation(Guid.NewGuid(), Guid.NewGuid(), ReservationStatus.Completed);
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var act = async () => await sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI });

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*completed*");
    }

    [Fact]
    public async Task CreatePaymentAsync_ExpiredPendingReservation_ThrowsPaymentException()
    {
        // Arrange
        var reservation = MakeReservation(Guid.NewGuid(), Guid.NewGuid(), ReservationStatus.Pending);
        reservation.ExpiryTime = DateTime.UtcNow.AddMinutes(-5); // expired
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var act = async () => await sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI });

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*expired*");
    }

    // ── TransactionService: ValidateDirectRefund branches ────────────────────

    [Fact]
    public async Task DirectGuestRefundAsync_WrongUser_ThrowsUnAuthorizedException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var reservation = MakeReservation(ownerId, Guid.NewGuid(), ReservationStatus.Confirmed);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow.AddMinutes(-5), Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act — different user tries to refund
        var act = async () => await sut.DirectGuestRefundAsync(tx.TransactionId, otherId, new RefundRequestDto { Reason = "Fraud" });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task DirectGuestRefundAsync_FailedTransaction_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Confirmed);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Failed, TransactionDate = DateTime.UtcNow.AddMinutes(-5), Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var act = async () => await sut.DirectGuestRefundAsync(tx.TransactionId, userId, new RefundRequestDto { Reason = "Error" });

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*successful*");
    }

    [Fact]
    public async Task DirectGuestRefundAsync_CompletedReservation_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Completed);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow.AddMinutes(-5), Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var act = async () => await sut.DirectGuestRefundAsync(tx.TransactionId, userId, new RefundRequestDto { Reason = "Error" });

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*Completed*");
    }

    [Fact]
    public async Task DirectGuestRefundAsync_CancelledReservation_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Cancelled);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow.AddMinutes(-5), Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var act = async () => await sut.DirectGuestRefundAsync(tx.TransactionId, userId, new RefundRequestDto { Reason = "Error" });

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*already cancelled*");
    }

    // ── TransactionService: MarkTransactionFailedAsync branches ──────────────

    [Fact]
    public async Task MarkTransactionFailedAsync_NonSuccessTransaction_ThrowsValidationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var reservation = MakeReservation(Guid.NewGuid(), hotelId, ReservationStatus.Confirmed);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Failed, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        _rrRepo.Setup(r => r.GetQueryable()).Returns(new List<ReservationRoom>().AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var act = async () => await sut.MarkTransactionFailedAsync(tx.TransactionId, adminId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*successful*");
    }

    [Fact]
    public async Task MarkTransactionFailedAsync_WrongHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var adminHotelId = Guid.NewGuid();
        var otherHotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = adminHotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var reservation = MakeReservation(Guid.NewGuid(), otherHotelId, ReservationStatus.Confirmed);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        var act = async () => await sut.MarkTransactionFailedAsync(tx.TransactionId, adminId);

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task MarkTransactionFailedAsync_ValidTransaction_RestoresInventoryAndSaves()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var reservation = MakeReservation(Guid.NewGuid(), hotelId, ReservationStatus.Confirmed);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        _rrRepo.Setup(r => r.GetQueryable()).Returns(new List<ReservationRoom>().AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        await sut.MarkTransactionFailedAsync(tx.TransactionId, adminId);

        // Assert
        tx.Status.Should().Be(PaymentStatus.Failed);
        reservation.Status.Should().Be(ReservationStatus.Pending);
        _uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── TransactionService: RestoreInventoryForReservationAsync with rooms ────

    [Fact]
    public async Task DirectGuestRefundAsync_WithReservationRooms_RestoresInventory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Confirmed);
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow.AddMinutes(-5), Reservation = reservation };
        reservation.Transactions = new List<Transaction> { tx };
        var rr = new ReservationRoom { ReservationRoomId = Guid.NewGuid(), ReservationId = reservation.ReservationId, RoomId = Guid.NewGuid(), RoomTypeId = roomTypeId };
        var inv = new RoomTypeInventory { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = reservation.CheckInDate, TotalInventory = 5, ReservedInventory = 2 };
        _txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        _rrRepo.Setup(r => r.GetQueryable()).Returns(new List<ReservationRoom> { rr }.AsQueryable().BuildMock());
        _invRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomTypeInventory> { inv }.AsQueryable().BuildMock());
        var sut = CreateTxSut();

        // Act
        await sut.DirectGuestRefundAsync(tx.TransactionId, userId, new RefundRequestDto { Reason = "Changed mind" });

        // Assert — inventory restored
        inv.ReservedInventory.Should().Be(1);
    }

    // ── ReviewService: GetMyReviewsPagedAsync (MapToMyDto) ───────────────────

    [Fact]
    public async Task ReviewService_GetMyReviewsPagedAsync_ReturnsMyReviews()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow };
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "RES-001", UserId = userId, HotelId = hotelId, Status = ReservationStatus.Completed, TotalAmount = 1000, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow };
        var review = new Review { ReviewId = Guid.NewGuid(), UserId = userId, HotelId = hotelId, ReservationId = reservationId, Rating = 4, Comment = "Great stay!", CreatedDate = DateTime.UtcNow, Hotel = hotel, Reservation = reservation };
        var reviewRepo = new Mock<IRepository<Guid, Review>>();
        reviewRepo.Setup(r => r.GetQueryable()).Returns(new List<Review> { review }.AsQueryable().BuildMock());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ReviewSettings:RewardPoints"] = "10" })
            .Build();

        var sut = new ReviewService(reviewRepo.Object,
            new Mock<IRepository<Guid, Hotel>>().Object,
            new Mock<IRepository<Guid, Reservation>>().Object,
            new Mock<IRepository<Guid, User>>().Object,
            new Mock<IWalletService>().Object,
            new Mock<IUnitOfWork>().Object,
            config);

        // Act
        var result = await sut.GetMyReviewsPagedAsync(userId, 1, 10);

        // Assert — exercises MapToMyDto
        result.TotalCount.Should().Be(1);
        result.Reviews.First().HotelName.Should().Be("Grand Hotel");
        result.Reviews.First().ReservationCode.Should().Be("RES-001");
    }

    // ── ReservationService: ValidateDatesAsync branches ──────────────────────

    [Fact]
    public async Task ReservationService_CreateReservation_CheckInToday_ThrowsValidationException()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_CheckInToday_ThrowsValidationException));
        var sut = new ReservationService(
            new HotelBookingAppWebApi.Repository.Repository<Guid, Reservation>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Room>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomType>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeInventory>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeRate>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, ReservationRoom>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Hotel>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, User>(ctx),
            new Mock<IWalletService>().Object,
            new Mock<IPromoCodeService>().Object,
            new Mock<ISuperAdminRevenueService>().Object,
            new HotelBookingAppWebApi.Services.UnitOfWork(ctx));

        // Act — check-in today (not tomorrow)
        var act = async () => await sut.CreateReservationAsync(Guid.NewGuid(),
            new HotelBookingAppWebApi.Models.DTOs.Reservation.CreateReservationDto
            {
                HotelId = Guid.NewGuid(), RoomTypeId = Guid.NewGuid(),
                CheckInDate = DateOnly.FromDateTime(DateTime.Now),
                CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                NumberOfRooms = 1
            });

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*tomorrow*");
    }

    [Fact]
    public async Task ReservationService_CreateReservation_CheckOutBeforeCheckIn_ThrowsValidationException()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_CheckOutBeforeCheckIn_ThrowsValidationException));
        var sut = new ReservationService(
            new HotelBookingAppWebApi.Repository.Repository<Guid, Reservation>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Room>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomType>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeInventory>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeRate>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, ReservationRoom>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Hotel>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, User>(ctx),
            new Mock<IWalletService>().Object,
            new Mock<IPromoCodeService>().Object,
            new Mock<ISuperAdminRevenueService>().Object,
            new HotelBookingAppWebApi.Services.UnitOfWork(ctx));

        // Act — checkout same as checkin
        var act = async () => await sut.CreateReservationAsync(Guid.NewGuid(),
            new HotelBookingAppWebApi.Models.DTOs.Reservation.CreateReservationDto
            {
                HotelId = Guid.NewGuid(), RoomTypeId = Guid.NewGuid(),
                CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                NumberOfRooms = 1
            });

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*after check-in*");
    }

    [Fact]
    public async Task ReservationService_CreateReservation_ZeroRooms_ThrowsValidationException()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_ZeroRooms_ThrowsValidationException));
        var sut = new ReservationService(
            new HotelBookingAppWebApi.Repository.Repository<Guid, Reservation>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Room>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomType>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeInventory>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeRate>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, ReservationRoom>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Hotel>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, User>(ctx),
            new Mock<IWalletService>().Object,
            new Mock<IPromoCodeService>().Object,
            new Mock<ISuperAdminRevenueService>().Object,
            new HotelBookingAppWebApi.Services.UnitOfWork(ctx));

        // Act — zero rooms
        var act = async () => await sut.CreateReservationAsync(Guid.NewGuid(),
            new HotelBookingAppWebApi.Models.DTOs.Reservation.CreateReservationDto
            {
                HotelId = Guid.NewGuid(), RoomTypeId = Guid.NewGuid(),
                CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                NumberOfRooms = 0
            });

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*at least 1*");
    }

    // ── ReservationService: MapToDetailsDto — CancellationFeePaid branch ─────

    [Fact]
    public async Task ReservationService_GetReservationByCode_CancellationFeePaid_ShowsCorrectPolicy()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_GetReservationByCode_CancellationFeePaid_ShowsCorrectPolicy));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow };
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES-FEE01",
            UserId = userId, HotelId = hotelId, Hotel = hotel,
            Status = ReservationStatus.Confirmed, TotalAmount = 1000, FinalAmount = 1000,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            CreatedDate = DateTime.UtcNow,
            CancellationFeePaid = true, // triggers the "fee paid" policy branch
            ReservationRooms = new List<ReservationRoom>()
        };
        ctx.Hotels.Add(hotel);
        ctx.Reservations.Add(reservation);
        await ctx.SaveChangesAsync();
        var sut = new ReservationService(
            new HotelBookingAppWebApi.Repository.Repository<Guid, Reservation>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Room>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomType>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeInventory>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeRate>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, ReservationRoom>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, Hotel>(ctx),
            new HotelBookingAppWebApi.Repository.Repository<Guid, User>(ctx),
            new Mock<IWalletService>().Object,
            new Mock<IPromoCodeService>().Object,
            new Mock<ISuperAdminRevenueService>().Object,
            new HotelBookingAppWebApi.Services.UnitOfWork(ctx));

        // Act
        var result = await sut.GetReservationByCodeAsync(userId, "RES-FEE01");

        // Assert — exercises the CancellationFeePaid=true branch in MapToDetailsDto
        result.CancellationPolicyText.Should().Contain("protection fee paid");
    }

    // ── TransactionService: BuildFailedTransaction FinalAmount=0 branch ──────

    [Fact]
    public async Task RecordFailedPaymentAsync_ZeroFinalAmount_UsesTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Pending);
        reservation.FinalAmount = 0; // triggers TotalAmount fallback
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);
        var sut = CreateTxSut();

        // Act
        await sut.RecordFailedPaymentAsync(reservation.ReservationId, userId);

        // Assert — amount should be TotalAmount (1000) not FinalAmount (0)
        _txRepo.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Amount == 1000)), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentAsync_ZeroFinalAmount_UsesTotalAmount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakeReservation(userId, Guid.NewGuid(), ReservationStatus.Pending);
        reservation.FinalAmount = 0; // triggers TotalAmount fallback in BuildSuccessTransaction
        _resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);
        var sut = CreateTxSut();

        // Act
        var result = await sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI });

        // Assert
        result.Amount.Should().Be(1000);
    }
}
