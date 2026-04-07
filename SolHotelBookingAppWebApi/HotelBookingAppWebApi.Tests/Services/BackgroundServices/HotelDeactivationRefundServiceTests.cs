using FluentAssertions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services.BackgroundServices;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services.BackgroundServices;

public class HotelDeactivationRefundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ILogger<HotelDeactivationRefundService>> _loggerMock = new();

    [Fact]
    public async Task ExecuteAsync_CancelledImmediately_DoesNotProcess()
    {
        // Arrange
        var sut = new HotelDeactivationRefundService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await sut.StartAsync(cts.Token);

        // Assert
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoAffectedReservations_DoesNotCommit()
    {
        // Arrange
        var reservationRepoMock = new Mock<IRepository<Guid, Reservation>>();
        var inventoryRepoMock = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var walletServiceMock = new Mock<IWalletService>();

        var emptyQueryable = new List<Reservation>().AsQueryable().BuildMock();
        reservationRepoMock.Setup(r => r.GetQueryable()).Returns(emptyQueryable);

        var scopeMock = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, Reservation>))).Returns(reservationRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, RoomTypeInventory>))).Returns(inventoryRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IUnitOfWork))).Returns(unitOfWorkMock.Object);
        spMock.Setup(p => p.GetService(typeof(IWalletService))).Returns(walletServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var sut = new HotelDeactivationRefundService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(300);

        // Assert
        unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DeactivatedHotelReservations_CancelsAndRefunds()
    {
        // Arrange
        var reservationRepoMock = new Mock<IRepository<Guid, Reservation>>();
        var inventoryRepoMock = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var walletServiceMock = new Mock<IWalletService>();

        var hotel = new Hotel { HotelId = Guid.NewGuid(), Name = "H1", Address = "A", City = "C",
            ContactNumber = "1234567890", IsActive = false, CreatedAt = DateTime.UtcNow };
        var successTx = new Transaction
        {
            TransactionId = Guid.NewGuid(), Amount = 500, Status = PaymentStatus.Success,
            PaymentMethod = PaymentMethod.UPI, TransactionDate = DateTime.UtcNow
        };
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES002",
            UserId = Guid.NewGuid(), HotelId = hotel.HotelId,
            Status = ReservationStatus.Confirmed,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            TotalAmount = 500, FinalAmount = 500, CreatedDate = DateTime.UtcNow,
            Hotel = hotel,
            ReservationRooms = new List<ReservationRoom>(),
            Transactions = new List<Transaction> { successTx }
        };

        var queryable = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        reservationRepoMock.Setup(r => r.GetQueryable()).Returns(queryable);

        var emptyInventory = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(emptyInventory);

        // BeginTransactionAsync must be set up so CommitAsync is reached
        unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        walletServiceMock.Setup(w => w.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var scopeMock = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, Reservation>))).Returns(reservationRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, RoomTypeInventory>))).Returns(inventoryRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IUnitOfWork))).Returns(unitOfWorkMock.Object);
        spMock.Setup(p => p.GetService(typeof(IWalletService))).Returns(walletServiceMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var sut = new HotelDeactivationRefundService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(400);

        // Assert
        unitOfWorkMock.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessingThrows_LogsErrorAndContinues()
    {
        // Arrange — use a local scope factory mock to avoid polluting the shared class-level mock
        var localScopeFactory = new Mock<IServiceScopeFactory>();
        localScopeFactory.Setup(f => f.CreateScope()).Throws(new Exception("DB error"));
        var sut = new HotelDeactivationRefundService(localScopeFactory.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        Func<Task> act = async () =>
        {
            await sut.StartAsync(cts.Token);
            await Task.Delay(300);
        };

        // Assert
        await act.Should().NotThrowAsync();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}
