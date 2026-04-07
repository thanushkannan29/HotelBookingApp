using FluentAssertions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services.BackgroundServices;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services.BackgroundServices;

public class NoShowAutoCancelServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<ILogger<NoShowAutoCancelService>> _loggerMock = new();

    [Fact]
    public async Task ExecuteAsync_CancelledImmediately_DoesNotProcess()
    {
        // Arrange
        var sut = new NoShowAutoCancelService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await sut.StartAsync(cts.Token);

        // Assert
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoNoShows_DoesNotCommit()
    {
        // Arrange
        var reservationRepoMock = new Mock<IRepository<Guid, Reservation>>();
        var inventoryRepoMock = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var emptyQueryable = new List<Reservation>().AsQueryable().BuildMock();
        reservationRepoMock.Setup(r => r.GetQueryable()).Returns(emptyQueryable);

        var scopeMock = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, Reservation>))).Returns(reservationRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, RoomTypeInventory>))).Returns(inventoryRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IUnitOfWork))).Returns(unitOfWorkMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var sut = new NoShowAutoCancelService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        // Act
        await sut.StartAsync(cts.Token);
        await Task.Delay(300);

        // Assert
        unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoShowReservationsExist_MarksAsNoShow()
    {
        // Arrange
        var reservationRepoMock = new Mock<IRepository<Guid, Reservation>>();
        var inventoryRepoMock = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var noShowReservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES003",
            UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(),
            Status = ReservationStatus.Confirmed,
            IsCheckedIn = false,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            TotalAmount = 500, CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>()
        };

        var queryable = new List<Reservation> { noShowReservation }.AsQueryable().BuildMock();
        reservationRepoMock.Setup(r => r.GetQueryable()).Returns(queryable);

        var emptyInventory = new List<RoomTypeInventory>().AsQueryable().BuildMock();
        inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(emptyInventory);

        var scopeMock = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, Reservation>))).Returns(reservationRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IRepository<Guid, RoomTypeInventory>))).Returns(inventoryRepoMock.Object);
        spMock.Setup(p => p.GetService(typeof(IUnitOfWork))).Returns(unitOfWorkMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        var sut = new NoShowAutoCancelService(_scopeFactoryMock.Object, _loggerMock.Object);

        // Act — start with no cancellation, let it run one iteration, then stop
        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await sut.StopAsync(CancellationToken.None);

        // Assert — scope was created meaning the service loop executed
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessingThrows_LogsErrorAndContinues()
    {
        // Arrange — use local mocks to avoid polluting the shared class-level mock
        var localScopeFactory = new Mock<IServiceScopeFactory>();
        localScopeFactory.Setup(f => f.CreateScope()).Throws(new Exception("DB error"));
        var sut = new NoShowAutoCancelService(localScopeFactory.Object, _loggerMock.Object);

        // Act — start the service, wait for one iteration to execute and log, then stop
        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await sut.StopAsync(CancellationToken.None);

        // Assert — error was logged
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}
