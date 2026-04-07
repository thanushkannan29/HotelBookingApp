using FluentAssertions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class PromoCodeServiceTests
{
    private readonly Mock<IRepository<Guid, PromoCode>> _promoRepoMock = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private PromoCodeService CreateSut() => new(
        _promoRepoMock.Object, _reservationRepoMock.Object,
        _hotelRepoMock.Object, _unitOfWorkMock.Object);

    private static PromoCode MakePromo(Guid userId, Guid hotelId, bool isUsed = false, int daysUntilExpiry = 30) => new()
    {
        PromoCodeId = Guid.NewGuid(), Code = "PROMO-TEST01",
        UserId = userId, HotelId = hotelId,
        DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry),
        IsUsed = isUsed, CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetGuestPromoCodesPagedAsync_ValidUser_ReturnsPaged()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode> { MakePromo(userId, hotelId) }.AsQueryable().BuildMock();
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = CreateSut();

        // Act
        var result = await sut.GetGuestPromoCodesPagedAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGuestPromoCodesPagedAsync_WithActiveFilter_ReturnsOnlyActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            MakePromo(userId, hotelId, isUsed: false, daysUntilExpiry: 30),
            MakePromo(userId, hotelId, isUsed: true, daysUntilExpiry: 30)
        }.AsQueryable().BuildMock();
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = CreateSut();

        // Act
        var result = await sut.GetGuestPromoCodesPagedAsync(userId, 1, 10, "Active");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_ValidPromo_ReturnsValidResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promo = MakePromo(userId, hotelId);
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = CreateSut();
        var dto = new ValidatePromoCodeDto { Code = "PROMO-TEST01", HotelId = hotelId, TotalAmount = 1000 };

        // Act
        var result = await sut.ValidateAsync(userId, dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DiscountPercent.Should().Be(10);
        result.DiscountAmount.Should().Be(100);
    }

    [Fact]
    public async Task ValidateAsync_PromoNotFound_ReturnsInvalid()
    {
        // Arrange
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        var sut = CreateSut();

        // Act
        var result = await sut.ValidateAsync(Guid.NewGuid(), new ValidatePromoCodeDto { Code = "INVALID", HotelId = Guid.NewGuid(), TotalAmount = 1000 });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateAsync_UsedPromo_ReturnsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promo = MakePromo(userId, hotelId, isUsed: true);
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = CreateSut();

        // Act
        var result = await sut.ValidateAsync(userId, new ValidatePromoCodeDto { Code = "PROMO-TEST01", HotelId = hotelId, TotalAmount = 1000 });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("already been used");
    }

    [Fact]
    public async Task ValidateAsync_ExpiredPromo_ReturnsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promo = MakePromo(userId, hotelId, isUsed: false, daysUntilExpiry: -5);
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = CreateSut();

        // Act
        var result = await sut.ValidateAsync(userId, new ValidatePromoCodeDto { Code = "PROMO-TEST01", HotelId = hotelId, TotalAmount = 1000 });

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task GeneratePromoForCompletedReservationAsync_NewReservation_CreatesPromo()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "R1", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 1500, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow };
        _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        _promoRepoMock.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).ReturnsAsync((PromoCode p) => p);
        var sut = CreateSut();

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(reservationId);

        // Assert
        _promoRepoMock.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Once);
    }

    [Fact]
    public async Task MarkUsedAsync_ValidCode_MarksAsUsed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var promo = MakePromo(userId, Guid.NewGuid());
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepoMock.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = CreateSut();

        // Act
        await sut.MarkUsedAsync("PROMO-TEST01", userId);

        // Assert
        promo.IsUsed.Should().BeTrue();
    }
}
