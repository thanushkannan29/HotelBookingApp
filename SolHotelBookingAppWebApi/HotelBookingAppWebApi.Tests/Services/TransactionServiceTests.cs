using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class TransactionServiceTests
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

    public TransactionServiceTests()
    {
        _sut = new TransactionService(
            _transactionRepo.Object, _reservationRepo.Object, _inventoryRepo.Object,
            _reservationRoomRepo.Object, _userRepo.Object, _hotelRepo.Object,
            _walletRepo.Object, _walletTxRepo.Object, _revenueRepo.Object, _unitOfWork.Object);
    }

    private static Reservation MakePendingReservation(Guid? userId = null, Guid? hotelId = null) => new()
    {
        ReservationId = Guid.NewGuid(),
        ReservationCode = "RES001",
        UserId = userId ?? Guid.NewGuid(),
        HotelId = hotelId ?? Guid.NewGuid(),
        Status = ReservationStatus.Pending,
        TotalAmount = 1000m,
        FinalAmount = 1000m,
        CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
        CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(4)),
        CreatedDate = DateTime.UtcNow,
        ExpiryTime = DateTime.UtcNow.AddMinutes(10),
        Transactions = new List<Transaction>()
    };

    // ── CreatePaymentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePaymentAsync_PendingReservation_ConfirmsAndReturnsTransaction()
    {
        // Arrange
        var reservation = MakePendingReservation();
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);
        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);

        var dto = new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI };

        // Act
        var result = await _sut.CreatePaymentAsync(dto);

        // Assert
        result.Amount.Should().Be(1000m);
        result.Status.Should().Be(PaymentStatus.Success);
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.CreatePaymentAsync(new CreatePaymentDto { ReservationId = Guid.NewGuid(), PaymentMethod = PaymentMethod.UPI }))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreatePaymentAsync_CancelledReservation_ThrowsPaymentException()
    {
        // Arrange
        var reservation = MakePendingReservation();
        reservation.Status = ReservationStatus.Cancelled;
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI }))
            .Should().ThrowAsync<PaymentException>().WithMessage("*cancelled*");
    }

    [Fact]
    public async Task CreatePaymentAsync_CompletedReservation_ThrowsPaymentException()
    {
        // Arrange
        var reservation = MakePendingReservation();
        reservation.Status = ReservationStatus.Completed;
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI }))
            .Should().ThrowAsync<PaymentException>().WithMessage("*completed*");
    }

    [Fact]
    public async Task CreatePaymentAsync_ExpiredReservation_ThrowsPaymentException()
    {
        // Arrange
        var reservation = MakePendingReservation();
        reservation.ExpiryTime = DateTime.UtcNow.AddMinutes(-5);
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI }))
            .Should().ThrowAsync<PaymentException>().WithMessage("*expired*");
    }

    [Fact]
    public async Task CreatePaymentAsync_AlreadyPaid_ThrowsPaymentException()
    {
        // Arrange
        var reservation = MakePendingReservation();
        reservation.Transactions = new List<Transaction>
        {
            new() { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow }
        };
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI }))
            .Should().ThrowAsync<PaymentException>().WithMessage("*already been paid*");
    }

    // ── DirectGuestRefundAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DirectGuestRefundAsync_WithinWindow_RefundsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation
        {
            ReservationId = reservationId,
            ReservationCode = "RES001",
            UserId = userId,
            HotelId = Guid.NewGuid(),
            Status = ReservationStatus.Confirmed,
            TotalAmount = 1000,
            CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(4)),
            CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>()
        };
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservationId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow.AddMinutes(-10),
            Reservation = reservation
        };
        reservation.Transactions = new List<Transaction> { transaction };

        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        var inventories = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        _inventoryRepo.Setup(r => r.GetQueryable()).Returns(inventories);

        // Act
        var result = await _sut.DirectGuestRefundAsync(transaction.TransactionId, userId, new RefundRequestDto { Reason = "Changed mind" });

        // Assert
        result.Status.Should().Be(PaymentStatus.Refunded);
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DirectGuestRefundAsync_TransactionNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act & Assert
        await _sut.Invoking(s => s.DirectGuestRefundAsync(Guid.NewGuid(), Guid.NewGuid(), new RefundRequestDto { Reason = "test" }))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DirectGuestRefundAsync_NotOwner_ThrowsUnauthorized()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var reservation = MakePendingReservation(ownerId);
        reservation.Status = ReservationStatus.Confirmed;
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow.AddMinutes(-5),
            Reservation = reservation
        };
        reservation.Transactions = new List<Transaction> { transaction };

        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act & Assert
        await _sut.Invoking(s => s.DirectGuestRefundAsync(transaction.TransactionId, Guid.NewGuid(), new RefundRequestDto { Reason = "test" }))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task DirectGuestRefundAsync_AfterWindow_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId);
        reservation.Status = ReservationStatus.Confirmed;
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow.AddMinutes(-35),
            Reservation = reservation
        };
        reservation.Transactions = new List<Transaction> { transaction };

        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act & Assert
        await _sut.Invoking(s => s.DirectGuestRefundAsync(transaction.TransactionId, userId, new RefundRequestDto { Reason = "late" }))
            .Should().ThrowAsync<PaymentException>().WithMessage("*expired*");
    }

    [Fact]
    public async Task DirectGuestRefundAsync_AlreadyCancelled_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId);
        reservation.Status = ReservationStatus.Cancelled;
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow.AddMinutes(-5),
            Reservation = reservation
        };
        reservation.Transactions = new List<Transaction> { transaction };

        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act & Assert
        await _sut.Invoking(s => s.DirectGuestRefundAsync(transaction.TransactionId, userId, new RefundRequestDto { Reason = "test" }))
            .Should().ThrowAsync<PaymentException>().WithMessage("*already cancelled*");
    }

    // ── GetPaymentIntentAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPaymentIntentAsync_PendingReservation_ReturnsIntent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "Grand", UpiId = "hotel@upi", Address = "A", City = "C", ContactNumber = "123", CreatedAt = DateTime.UtcNow };
        var reservation = MakePendingReservation(userId, hotel.HotelId);
        reservation.Hotel = hotel;

        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act
        var result = await _sut.GetPaymentIntentAsync(reservation.ReservationId, userId);

        // Assert
        result.UpiId.Should().Be("hotel@upi");
        result.Amount.Should().Be(1000m);
        result.PaymentRef.Should().Contain("RES001");
    }

    [Fact]
    public async Task GetPaymentIntentAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.GetPaymentIntentAsync(Guid.NewGuid(), Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPaymentIntentAsync_ConfirmedReservation_ThrowsValidationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId);
        reservation.Status = ReservationStatus.Confirmed;

        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.GetPaymentIntentAsync(reservation.ReservationId, userId))
            .Should().ThrowAsync<ValidationException>().WithMessage("*pending*");
    }

    // ── MarkTransactionFailedAsync ────────────────────────────────────────────

    [Fact]
    public async Task MarkTransactionFailedAsync_SuccessfulTransaction_MarksFailedAndResetsPending()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var reservation = MakePendingReservation(null, hotelId);
        reservation.Status = ReservationStatus.Confirmed;
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow,
            Reservation = reservation
        };

        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        var reservationRooms = new List<ReservationRoom>().AsQueryable().BuildMock();
        _reservationRoomRepo.Setup(r => r.GetQueryable()).Returns(reservationRooms);

        // Act
        await _sut.MarkTransactionFailedAsync(transaction.TransactionId, adminId);

        // Assert
        transaction.Status.Should().Be(PaymentStatus.Failed);
        reservation.Status.Should().Be(ReservationStatus.Pending);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkTransactionFailedAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.MarkTransactionFailedAsync(Guid.NewGuid(), Guid.NewGuid()))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task MarkTransactionFailedAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.MarkTransactionFailedAsync(Guid.NewGuid(), admin.UserId))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task MarkTransactionFailedAsync_TransactionNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act & Assert
        await _sut.Invoking(s => s.MarkTransactionFailedAsync(Guid.NewGuid(), adminId))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MarkTransactionFailedAsync_WrongHotel_ThrowsUnauthorized()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var adminHotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = adminHotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var reservation = MakePendingReservation(null, Guid.NewGuid()); // different hotel
        reservation.Status = ReservationStatus.Confirmed;
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow,
            Reservation = reservation
        };

        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act & Assert
        await _sut.Invoking(s => s.MarkTransactionFailedAsync(transaction.TransactionId, adminId))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task MarkTransactionFailedAsync_AlreadyFailed_ThrowsValidationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = new User { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

        var reservation = MakePendingReservation(null, hotelId);
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            ReservationId = reservation.ReservationId,
            Amount = 1000,
            PaymentMethod = PaymentMethod.UPI,
            Status = PaymentStatus.Failed,
            TransactionDate = DateTime.UtcNow,
            Reservation = reservation
        };

        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act & Assert
        await _sut.Invoking(s => s.MarkTransactionFailedAsync(transaction.TransactionId, adminId))
            .Should().ThrowAsync<ValidationException>().WithMessage("*successful*");
    }

    // ── RecordFailedPaymentAsync ──────────────────────────────────────────────

    [Fact]
    public async Task RecordFailedPaymentAsync_ValidReservation_RecordsFailedTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId);
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);
        _transactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);

        // Act
        await _sut.RecordFailedPaymentAsync(reservation.ReservationId, userId);

        // Assert
        _transactionRepo.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Status == PaymentStatus.Failed &&
            t.ReservationId == reservation.ReservationId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RecordFailedPaymentAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        // Act & Assert
        await _sut.Invoking(s => s.RecordFailedPaymentAsync(Guid.NewGuid(), Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }
}
