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
    private readonly Mock<IRepository<Guid, PromoCode>> _promoRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepo = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PromoCodeService _sut;

    public PromoCodeServiceTests()
    {
        _sut = new PromoCodeService(_promoRepo.Object, _reservationRepo.Object, _hotelRepo.Object, _unitOfWork.Object);
    }

    private static PromoCode MakePromo(Guid userId, Guid hotelId, bool isUsed = false, DateTime? expiry = null)
        => new()
        {
            PromoCodeId = Guid.NewGuid(),
            Code = "PROMO-ABCD1234",
            UserId = userId,
            HotelId = hotelId,
            ReservationId = Guid.NewGuid(),
            DiscountPercent = 10,
            ExpiryDate = expiry ?? DateTime.UtcNow.AddDays(30),
            IsUsed = isUsed,
            CreatedAt = DateTime.UtcNow,
            Hotel = new Hotel { HotelId = hotelId, Name = "Grand", Address = "A", City = "C", ContactNumber = "123", CreatedAt = DateTime.UtcNow }
        };

    // ── GetGuestPromoCodesAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetGuestPromoCodesAsync_ReturnsUserPromos()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode> { MakePromo(userId, hotelId) }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.GetGuestPromoCodesAsync(userId);

        result.Should().HaveCount(1);
    }

    // ── GetGuestPromoCodesPagedAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetGuestPromoCodesPagedAsync_NoFilter_ReturnsAll()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode> { MakePromo(userId, hotelId) }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.GetGuestPromoCodesPagedAsync(userId, 1, 10);

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGuestPromoCodesPagedAsync_ActiveFilter_ReturnsOnlyActive()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            MakePromo(userId, hotelId, isUsed: false, expiry: DateTime.UtcNow.AddDays(10)),
            MakePromo(userId, hotelId, isUsed: true)
        }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.GetGuestPromoCodesPagedAsync(userId, 1, 10, "Active");

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGuestPromoCodesPagedAsync_UsedFilter_ReturnsOnlyUsed()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            MakePromo(userId, hotelId, isUsed: false),
            MakePromo(userId, hotelId, isUsed: true)
        }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.GetGuestPromoCodesPagedAsync(userId, 1, 10, "Used");

        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGuestPromoCodesPagedAsync_ExpiredFilter_ReturnsOnlyExpired()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            MakePromo(userId, hotelId, isUsed: false, expiry: DateTime.UtcNow.AddDays(-1)),
            MakePromo(userId, hotelId, isUsed: false, expiry: DateTime.UtcNow.AddDays(10))
        }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.GetGuestPromoCodesPagedAsync(userId, 1, 10, "Expired");

        result.TotalCount.Should().Be(1);
    }

    // ── ValidateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidPromo_ReturnsValidResult()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promo = MakePromo(userId, hotelId);
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.ValidateAsync(userId, new ValidatePromoCodeDto { Code = promo.Code, HotelId = hotelId, TotalAmount = 1000 });

        result.IsValid.Should().BeTrue();
        result.DiscountPercent.Should().Be(10);
        result.DiscountAmount.Should().Be(100);
    }

    [Fact]
    public async Task ValidateAsync_PromoNotFound_ReturnsInvalid()
    {
        var promos = new List<PromoCode>().AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.ValidateAsync(Guid.NewGuid(), new ValidatePromoCodeDto { Code = "NONE", HotelId = Guid.NewGuid(), TotalAmount = 100 });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_UsedPromo_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promo = MakePromo(userId, hotelId, isUsed: true);
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.ValidateAsync(userId, new ValidatePromoCodeDto { Code = promo.Code, HotelId = hotelId, TotalAmount = 100 });

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("already been used");
    }

    [Fact]
    public async Task ValidateAsync_ExpiredPromo_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promo = MakePromo(userId, hotelId, expiry: DateTime.UtcNow.AddDays(-1));
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        var result = await _sut.ValidateAsync(userId, new ValidatePromoCodeDto { Code = promo.Code, HotelId = hotelId, TotalAmount = 100 });

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    // ── GeneratePromoForCompletedReservationAsync ─────────────────────────────

    [Fact]
    public async Task GeneratePromoForCompletedReservationAsync_NewReservation_CreatesPromo()
    {
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 1500, ReservationCode = "RES001", CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(3)), Status = ReservationStatus.Completed, CreatedDate = DateTime.UtcNow };
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);
        var existingPromos = new List<PromoCode>().AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(existingPromos);
        _promoRepo.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).ReturnsAsync(new PromoCode());

        await _sut.GeneratePromoForCompletedReservationAsync(reservationId);

        _promoRepo.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GeneratePromoForCompletedReservationAsync_AlreadyExists_SkipsCreation()
    {
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 500, ReservationCode = "RES002", CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)), Status = ReservationStatus.Completed, CreatedDate = DateTime.UtcNow };
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);
        var existingPromos = new List<PromoCode> { new() { ReservationId = reservationId, Code = "X", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), ExpiryDate = DateTime.UtcNow.AddDays(30), CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(existingPromos);

        await _sut.GeneratePromoForCompletedReservationAsync(reservationId);

        _promoRepo.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Never);
    }

    [Fact]
    public async Task GeneratePromoForCompletedReservationAsync_ReservationNotFound_DoesNothing()
    {
        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        await _sut.GeneratePromoForCompletedReservationAsync(Guid.NewGuid());

        _promoRepo.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Never);
    }

    // ── MarkUsedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkUsedAsync_ExistingPromo_MarksUsed()
    {
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promo = MakePromo(userId, hotelId);
        var promos = new List<PromoCode> { promo }.AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        await _sut.MarkUsedAsync(promo.Code, userId);

        promo.IsUsed.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkUsedAsync_PromoNotFound_DoesNothing()
    {
        var promos = new List<PromoCode>().AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(promos);

        await _sut.MarkUsedAsync("NOTEXIST", Guid.NewGuid());

        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ── Discount tier tests ───────────────────────────────────────────────────

    [Theory]
    [InlineData(400, 5)]
    [InlineData(800, 10)]
    [InlineData(1500, 15)]
    [InlineData(3000, 20)]
    [InlineData(6000, 25)]
    public async Task GeneratePromo_DiscountTiers_CorrectPercent(decimal amount, decimal expectedDiscount)
    {
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = amount, ReservationCode = "RES", CheckInDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), CheckOutDate = DateOnly.FromDateTime(DateTime.Now.AddDays(2)), Status = ReservationStatus.Completed, CreatedDate = DateTime.UtcNow };
        var reservations = new List<Reservation> { reservation }.AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);
        var existingPromos = new List<PromoCode>().AsQueryable().BuildMock();
        _promoRepo.Setup(r => r.GetQueryable()).Returns(existingPromos);
        PromoCode? captured = null;
        _promoRepo.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).Callback<PromoCode>(p => captured = p).ReturnsAsync(new PromoCode());

        await _sut.GeneratePromoForCompletedReservationAsync(reservationId);

        captured!.DiscountPercent.Should().Be(expectedDiscount);
    }
}
