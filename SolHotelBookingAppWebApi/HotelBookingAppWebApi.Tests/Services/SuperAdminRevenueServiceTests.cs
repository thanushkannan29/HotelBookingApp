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
    private readonly Mock<IRepository<Guid, SuperAdminRevenue>> _revenueRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepo = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SuperAdminRevenueService _sut;

    public SuperAdminRevenueServiceTests()
    {
        _sut = new SuperAdminRevenueService(
            _revenueRepo.Object, _reservationRepo.Object,
            _hotelRepo.Object, _unitOfWork.Object);
    }

    private static Reservation MakeReservation(decimal total = 1000m)
        => new()
        {
            ReservationId = Guid.NewGuid(),
            HotelId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ReservationCode = "RES001",
            TotalAmount = total,
            Status = ReservationStatus.Completed,
            CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
            CreatedDate = DateTime.UtcNow
        };

    // ── RecordCommissionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RecordCommissionAsync_NewReservation_RecordsCommission()
    {
        // Arrange
        var reservation = MakeReservation(1000m);
        var emptyRevenues = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(emptyRevenues);
        _reservationRepo.Setup(r => r.GetAsync(reservation.ReservationId)).ReturnsAsync(reservation);
        _revenueRepo.Setup(r => r.AddAsync(It.IsAny<SuperAdminRevenue>())).ReturnsAsync((SuperAdminRevenue rev) => rev);

        // Act
        await _sut.RecordCommissionAsync(reservation.ReservationId);

        // Assert
        _revenueRepo.Verify(r => r.AddAsync(It.Is<SuperAdminRevenue>(
            rev => rev.CommissionAmount == 20m && rev.ReservationId == reservation.ReservationId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RecordCommissionAsync_AlreadyExists_SkipsIdempotent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var existing = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = reservationId, HotelId = Guid.NewGuid(), CommissionAmount = 20m, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(existing);

        // Act
        await _sut.RecordCommissionAsync(reservationId);

        // Assert
        _revenueRepo.Verify(r => r.AddAsync(It.IsAny<SuperAdminRevenue>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RecordCommissionAsync_ReservationNotFound_ThrowsNotFound()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var emptyRevenues = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(emptyRevenues);
        _reservationRepo.Setup(r => r.GetAsync(reservationId)).ReturnsAsync((Reservation?)null);

        // Act & Assert
        await _sut.Invoking(s => s.RecordCommissionAsync(reservationId))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(500, 10)]
    [InlineData(2000, 40)]
    [InlineData(10000, 200)]
    public async Task RecordCommissionAsync_CalculatesCorrect2Percent(decimal total, decimal expectedCommission)
    {
        // Arrange
        var reservation = MakeReservation(total);
        var emptyRevenues = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(emptyRevenues);
        _reservationRepo.Setup(r => r.GetAsync(reservation.ReservationId)).ReturnsAsync(reservation);
        SuperAdminRevenue? captured = null;
        _revenueRepo.Setup(r => r.AddAsync(It.IsAny<SuperAdminRevenue>()))
            .Callback<SuperAdminRevenue>(rev => captured = rev)
            .ReturnsAsync(new SuperAdminRevenue());

        // Act
        await _sut.RecordCommissionAsync(reservation.ReservationId);

        // Assert
        captured!.CommissionAmount.Should().Be(expectedCommission);
    }

    // ── GetAllRevenueAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRevenueAsync_ReturnsPagedRevenue()
    {
        // Arrange
        var revenues = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), HotelId = Guid.NewGuid(), ReservationAmount = 1000, CommissionAmount = 20, SuperAdminUpiId = "admin@upi", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(revenues);

        // Act
        var result = await _sut.GetAllRevenueAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().CommissionAmount.Should().Be(20);
    }

    [Fact]
    public async Task GetAllRevenueAsync_EmptyData_ReturnsZeroCount()
    {
        // Arrange
        var revenues = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(revenues);

        // Act
        var result = await _sut.GetAllRevenueAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // ── GetSummaryAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_ReturnsTotalCommission()
    {
        // Arrange
        var revenues = new List<SuperAdminRevenue>
        {
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), HotelId = Guid.NewGuid(), CommissionAmount = 20m, CreatedAt = DateTime.UtcNow },
            new() { SuperAdminRevenueId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), HotelId = Guid.NewGuid(), CommissionAmount = 40m, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(revenues);

        // Act
        var result = await _sut.GetSummaryAsync();

        // Assert
        result.TotalCommissionEarned.Should().Be(60m);
    }

    [Fact]
    public async Task GetSummaryAsync_NoRevenue_ReturnsZero()
    {
        // Arrange
        var revenues = new List<SuperAdminRevenue>().AsQueryable().BuildMock();
        _revenueRepo.Setup(r => r.GetQueryable()).Returns(revenues);

        // Act
        var result = await _sut.GetSummaryAsync();

        // Assert
        result.TotalCommissionEarned.Should().Be(0m);
    }
}
