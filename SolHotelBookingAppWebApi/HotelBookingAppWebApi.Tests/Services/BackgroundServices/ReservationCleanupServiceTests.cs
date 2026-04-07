using FluentAssertions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services.BackgroundServices;

public class ReservationCleanupServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ILogger<ReservationCleanupService>> _loggerMock = new();

    [Fact]
    public async Task ExecuteAsync_CancelledImmediately_DoesNotProcess()
    {
        // Arrange
        var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await sut.StartAsync(cts.Token);

        // Assert
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoExpiredReservations_DoesNotCommit()
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

        var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(300);

        // Assert
        unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredReservationsExist_CancelsAndRefunds()
    {
        // Arrange
        var reservationRepoMock = new Mock<IRepository<Guid, Reservation>>();
        var inventoryRepoMock = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var walletServiceMock = new Mock<IWalletService>();

        var expiredReservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            HotelId = Guid.NewGuid(),
            Status = ReservationStatus.Pending,
            ExpiryTime = DateTime.UtcNow.AddMinutes(-10),
            WalletAmountUsed = 100m,
            ReservationCode = "RES001",
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            TotalAmount = 500,
            CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>()
        };

        var queryable = new List<Reservation> { expiredReservation }.AsQueryable().BuildMock();
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

        var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(300);

        // Assert
        unitOfWorkMock.Verify(u => u.CommitAsync(), Times.AtLeastOnce);
        walletServiceMock.Verify(
            w => w.CreditAsync(expiredReservation.UserId, 100m, It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessingThrows_LogsErrorAndContinues()
    {
        // Arrange
        _scopeFactoryMock.Setup(f => f.CreateScope()).Throws(new Exception("DB error"));
        var sut = new ReservationCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act — start the service, wait for the loop to execute and log, then verify
        await sut.StartAsync(cts.Token);
        await Task.Delay(800); // wait long enough for the loop to run at least once

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}
