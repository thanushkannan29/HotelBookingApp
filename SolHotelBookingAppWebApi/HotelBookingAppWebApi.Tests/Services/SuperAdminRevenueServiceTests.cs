using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class SuperAdminRevenueServiceTests
{
    private readonly Mock<IRepository<Guid, SuperAdminRevenue>> _revenueRepoMock = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private SuperAdminRevenueService CreateSut() => new(
        _revenueRepoMock.Object, _reservationRepoMock.Object,
        _hotelRepoMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task RecordCommissionAsync_CommissionIsOnFinalAmountNotTotalAmount()
    {
        // Arrange — reservation with ₹1000 base but ₹850 final (after GST + promo + wallet)
        var reservationId = Guid.NewGuid();
        var emptyRevenue = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(emptyRevenue);

        SuperAdminRevenue? captured = null;
        _revenueRepoMock.Setup(r => r.AddAsync(It.IsAny<SuperAdminRevenue>()))
            .Callback<SuperAdminRevenue>(sr => captured = sr)
            .ReturnsAsync((SuperAdminRevenue sr) => sr);

        var reservation = new Reservation
        {
            ReservationId = reservationId, ReservationCode = "R1",
            UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(),
            TotalAmount = 1000m,   // base before GST/discount
            FinalAmount = 850m,    // what guest actually paid
            Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CreatedDate = DateTime.UtcNow
        };
        _reservationRepoMock.Setup(r => r.GetAsync(reservationId)).ReturnsAsync(reservation);
        var sut = CreateSut();

        // Act
        await sut.RecordCommissionAsync(reservationId);

        // Assert — 2% of ₹850 (FinalAmount) = ₹17, NOT 2% of ₹1000 (TotalAmount) = ₹20
        captured.Should().NotBeNull();
        captured!.ReservationAmount.Should().Be(850m);
        captured.CommissionAmount.Should().Be(17m);
    }

    [Fact]
    public async Task RecordCommissionAsync_NewReservation_AddsRecord()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var emptyRevenue = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(emptyRevenue);
        var reservation = new Reservation
        {
            ReservationId = reservationId, ReservationCode = "R1",
            UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(),
            TotalAmount = 1000, Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CreatedDate = DateTime.UtcNow
        };
        _reservationRepoMock.Setup(r => r.GetAsync(reservationId)).ReturnsAsync(reservation);
        _revenueRepoMock.Setup(r => r.AddAsync(It.IsAny<SuperAdminRevenue>())).ReturnsAsync((SuperAdminRevenue sr) => sr);
        var sut = CreateSut();

        // Act
        await sut.RecordCommissionAsync(reservationId);

        // Assert
        _revenueRepoMock.Verify(r => r.AddAsync(It.IsAny<SuperAdminRevenue>()), Times.Once);
    }

    [Fact]
    public async Task RecordCommissionAsync_AlreadyRecorded_IsIdempotent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var existing = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = reservationId, HotelId = Guid.NewGuid(), ReservationAmount = 1000, CommissionAmount = 20, SuperAdminUpiId = "test@upi", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(existing);
        var sut = CreateSut();

        // Act
        await sut.RecordCommissionAsync(reservationId);

        // Assert
        _revenueRepoMock.Verify(r => r.AddAsync(It.IsAny<SuperAdminRevenue>()), Times.Never);
    }

    [Fact]
    public async Task RecordCommissionAsync_ReservationNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var emptyRevenue = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(emptyRevenue);
        _reservationRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Reservation?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.RecordCommissionAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllRevenueAsync_ValidPagination_ReturnsPaged()
    {
        // Arrange
        var revenue = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), HotelId = Guid.NewGuid(), ReservationAmount = 1000, CommissionAmount = 20, SuperAdminUpiId = "test@upi", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(revenue);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllRevenueAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsTotal()
    {
        // Arrange
        var revenue = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), HotelId = Guid.NewGuid(), ReservationAmount = 1000, CommissionAmount = 20, SuperAdminUpiId = "test@upi", CreatedAt = DateTime.UtcNow },
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), HotelId = Guid.NewGuid(), ReservationAmount = 2000, CommissionAmount = 40, SuperAdminUpiId = "test@upi", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _revenueRepoMock.Setup(r => r.GetQueryable()).Returns(revenue);
        var sut = CreateSut();

        // Act
        var result = await sut.GetSummaryAsync();

        // Assert
        result.TotalCommissionEarned.Should().Be(60m);
    }
}
