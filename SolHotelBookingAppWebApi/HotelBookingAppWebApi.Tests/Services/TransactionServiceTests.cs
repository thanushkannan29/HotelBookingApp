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

public class TransactionServiceTests
{
    private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
    private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
    private readonly Mock<IRepository<Guid, ReservationRoom>> _reservationRoomRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IRepository<Guid, Wallet>> _walletRepoMock = new();
    private readonly Mock<IRepository<Guid, WalletTransaction>> _walletTxRepoMock = new();
    private readonly Mock<IRepository<Guid, SuperAdminRevenue>> _revenueRepoMock = new();
    private readonly Mock<IWalletService> _walletServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private TransactionService CreateSut() => new(
        _transactionRepoMock.Object, _reservationRepoMock.Object,
        _inventoryRepoMock.Object, _reservationRoomRepoMock.Object,
        _userRepoMock.Object, _hotelRepoMock.Object,
        _walletRepoMock.Object, _walletTxRepoMock.Object,
        _revenueRepoMock.Object, _walletServiceMock.Object, _unitOfWorkMock.Object);

    private static Reservation MakePendingReservation(Guid userId, Guid hotelId) => new()
    {
        ReservationId = Guid.NewGuid(), ReservationCode = "R1",
        UserId = userId, HotelId = hotelId,
        Status = ReservationStatus.Pending,
        CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
        CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
        TotalAmount = 1000, FinalAmount = 1000,
        ExpiryTime = DateTime.UtcNow.AddHours(1),
        CreatedDate = DateTime.UtcNow,
        Transactions = new List<Transaction>()
    };

    [Fact]
    public async Task CreatePaymentAsync_ValidPendingReservation_ReturnsTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId, hotelId);
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);
        var sut = CreateSut();

        // Act
        var result = await sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI });

        // Assert
        result.Status.Should().Be(PaymentStatus.Success);
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task CreatePaymentAsync_CancelledReservation_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId, Guid.NewGuid());
        reservation.Status = ReservationStatus.Cancelled;
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI });

        // Assert
        await act.Should().ThrowAsync<PaymentException>();
    }

    [Fact]
    public async Task CreatePaymentAsync_AlreadyPaid_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId, Guid.NewGuid());
        reservation.Transactions = new List<Transaction>
        {
            new() { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow }
        };
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservation.ReservationId, PaymentMethod = PaymentMethod.UPI });

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*already been paid*");
    }

    [Fact]
    public async Task CreatePaymentAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var act = async () => await sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = Guid.NewGuid(), PaymentMethod = PaymentMethod.UPI });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task RecordFailedPaymentAsync_ValidReservation_AddsFailedTransaction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId, Guid.NewGuid());
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).ReturnsAsync((Transaction t) => t);
        var sut = CreateSut();

        // Act
        await sut.RecordFailedPaymentAsync(reservation.ReservationId, userId);

        // Assert
        _transactionRepoMock.Verify(r => r.AddAsync(It.Is<Transaction>(t => t.Status == PaymentStatus.Failed)), Times.Once);
    }

    [Fact]
    public async Task GetPaymentIntentAsync_ValidPendingReservation_ReturnsIntent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "1234567890", UpiId = "hotel@upi", CreatedAt = DateTime.UtcNow };
        var reservation = MakePendingReservation(userId, hotel.HotelId);
        reservation.Hotel = hotel;
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        var sut = CreateSut();

        // Act
        var result = await sut.GetPaymentIntentAsync(reservation.ReservationId, userId);

        // Assert
        result.UpiId.Should().Be("hotel@upi");
        result.Amount.Should().Be(1000);
    }

    [Fact]
    public async Task GetPaymentIntentAsync_ConfirmedReservation_ThrowsValidationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId, Guid.NewGuid());
        reservation.Status = ReservationStatus.Confirmed;
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(reservations);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetPaymentIntentAsync(reservation.ReservationId, userId);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task DirectGuestRefundAsync_WithinWindow_RefundsAndCancels()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId, Guid.NewGuid());
        reservation.Status = ReservationStatus.Confirmed;
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId,
            Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow.AddMinutes(-10), // within 30 min
            Reservation = reservation
        };
        reservation.Transactions = new List<Transaction> { transaction };
        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions);
        _reservationRoomRepoMock.Setup(r => r.GetQueryable()).Returns(new List<ReservationRoom>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.DirectGuestRefundAsync(transaction.TransactionId, userId, new RefundRequestDto { Reason = "Changed mind" });

        // Assert
        result.Status.Should().Be(PaymentStatus.Refunded);
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task DirectGuestRefundAsync_AfterWindow_ThrowsPaymentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservation = MakePendingReservation(userId, Guid.NewGuid());
        reservation.Status = ReservationStatus.Confirmed;
        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId,
            Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success,
            TransactionDate = DateTime.UtcNow.AddMinutes(-45), // outside 30 min window
            Reservation = reservation
        };
        reservation.Transactions = new List<Transaction> { transaction };
        var transactions = new List<Transaction> { transaction }.AsQueryable().BuildMock();
        _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(transactions);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.DirectGuestRefundAsync(transaction.TransactionId, userId, new RefundRequestDto { Reason = "Changed mind" });

        // Assert
        await act.Should().ThrowAsync<PaymentException>().WithMessage("*window*");
    }
}
