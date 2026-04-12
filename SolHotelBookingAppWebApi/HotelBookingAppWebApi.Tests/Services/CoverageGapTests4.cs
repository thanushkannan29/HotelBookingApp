using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using HotelBookingAppWebApi.Repository;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

/// <summary>
/// Final coverage gap tests targeting remaining uncovered lines.
/// </summary>
public class CoverageGapTests4
{
    private static HotelBookingContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HotelBookingContext(opts);
    }

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "admin@test.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    // ── WalletService: TopUpAsync rollback branch ─────────────────────────────

    [Fact]
    public async Task WalletService_TopUpAsync_ExceptionDuringCredit_RollsBack()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        var walletTxRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var uow = new Mock<IUnitOfWork>();
        var wallet = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 100m, UpdatedAt = DateTime.UtcNow };
        walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        // Force exception during AddAsync of wallet transaction
        walletTxRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ThrowsAsync(new Exception("DB error"));
        var sut = new WalletService(walletRepo.Object, walletTxRepo.Object, userRepo.Object, uow.Object);

        // Act
        var act = async () => await sut.TopUpAsync(userId, 50m);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── RoomService: UpdateRoomAsync rollback branch ──────────────────────────

    [Fact]
    public async Task RoomService_UpdateRoomAsync_RoomTypeNotFound_RollsBack()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RoomService_UpdateRoomAsync_RoomTypeNotFound_RollsBack));
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomId = Guid.NewGuid();
        var room = new Room { RoomId = roomId, HotelId = hotelId, RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid(), IsActive = true };
        ctx.Users.Add(admin);
        ctx.Rooms.Add(room);
        await ctx.SaveChangesAsync();
        var roomRepo = new Repository<Guid, Room>(ctx);
        var roomTypeRepo = new Repository<Guid, RoomType>(ctx);
        var invRepo = new Repository<Guid, RoomTypeInventory>(ctx);
        var userRepo = new Repository<Guid, User>(ctx);
        var uow = new Mock<IUnitOfWork>();
        var sut = new RoomService(roomRepo, roomTypeRepo, invRepo, userRepo, new Mock<IAuditLogService>().Object, uow.Object);

        // Act — use a RoomTypeId that doesn't exist in this hotel
        var act = async () => await sut.UpdateRoomAsync(admin.UserId, new UpdateRoomDto
        {
            RoomId = roomId, RoomNumber = "202", Floor = 2, RoomTypeId = Guid.NewGuid()
        });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── RoomTypeService: UpdateRoomTypeAsync with AmenityIds (ReplaceAmenityAssociationsAsync) ──

    [Fact]
    public async Task RoomTypeService_UpdateRoomTypeAsync_WithAmenityIds_ReplacesAssociations()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RoomTypeService_UpdateRoomTypeAsync_WithAmenityIds_ReplacesAssociations));
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomTypeId = Guid.NewGuid();
        var amenityId = Guid.NewGuid();
        ctx.Users.Add(admin);
        ctx.RoomTypes.Add(new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true });
        ctx.Amenities.Add(new Amenity { AmenityId = amenityId, Name = "Pool", Category = "Recreation", IsActive = true });
        await ctx.SaveChangesAsync();
        var sut = new RoomTypeService(
            new Repository<Guid, RoomType>(ctx),
            new Repository<Guid, RoomTypeRate>(ctx),
            new Repository<Guid, RoomTypeAmenity>(ctx),
            new Repository<Guid, User>(ctx),
            new Mock<IAuditLogService>().Object,
            new UnitOfWork(ctx), ctx);

        // Act — update with new amenity IDs triggers ReplaceAmenityAssociationsAsync
        await sut.UpdateRoomTypeAsync(admin.UserId, new UpdateRoomTypeDto
        {
            RoomTypeId = roomTypeId, Name = "Deluxe Updated", MaxOccupancy = 3,
            AmenityIds = new List<Guid> { amenityId }
        });

        // Assert
        var associations = ctx.RoomTypeAmenities.Where(rta => rta.RoomTypeId == roomTypeId).ToList();
        associations.Should().HaveCount(1);
        associations[0].AmenityId.Should().Be(amenityId);
    }

    // ── HotelService: GetAvailabilityAsync ────────────────────────────────────

    [Fact]
    public async Task HotelService_GetAvailabilityAsync_ValidHotel_ReturnsAvailability()
    {
        // Arrange — use in-memory DB since GetAvailabilityAsync uses complex EF navigation chains
        using var ctx = CreateContext(nameof(HotelService_GetAvailabilityAsync_ValidHotel_ReturnsAvailability));
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", IsActive = true, CreatedAt = DateTime.UtcNow };
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var rate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = checkIn.AddDays(-5), EndDate = checkOut.AddDays(5), Rate = 1500m };
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = checkIn, TotalInventory = 5, ReservedInventory = 1 };
        ctx.Hotels.Add(hotel);
        ctx.RoomTypes.Add(roomType);
        ctx.RoomTypeRates.Add(rate);
        ctx.RoomTypeInventories.Add(inventory);
        await ctx.SaveChangesAsync();
        var sut = new HotelService(
            new Repository<Guid, Hotel>(ctx),
            new Repository<Guid, User>(ctx),
            new Repository<Guid, RoomType>(ctx),
            new Repository<Guid, Transaction>(ctx),
            new Repository<Guid, Reservation>(ctx),
            new Mock<IAuditLogService>().Object,
            new UnitOfWork(ctx));

        // Act
        var result = await sut.GetAvailabilityAsync(hotelId, checkIn, checkOut);

        // Assert — exercises GetAvailabilityAsync and the LINQ GroupBy projection
        result.Should().NotBeNull();
    }

    // ── HotelService: GetAllHotelsForSuperAdminAsync MapToSuperAdminDto with revenue ──

    [Fact]
    public async Task HotelService_GetAllHotelsForSuperAdminAsync_WithRevenueAndReservations_MapsCorrectly()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", State = "MH", ContactNumber = "9999999999", IsActive = true, CreatedAt = DateTime.UtcNow };
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        var reservation = new Reservation { ReservationId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), Status = ReservationStatus.Confirmed, TotalAmount = 1000, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow };
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var tx = new Transaction { TransactionId = Guid.NewGuid(), ReservationId = reservation.ReservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = reservation };
        var txRepo = new Mock<IRepository<Guid, Transaction>>();
        txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction> { tx }.AsQueryable().BuildMock());
        var sut = new HotelService(hotelRepo.Object, new Mock<IRepository<Guid, User>>().Object,
            new Mock<IRepository<Guid, RoomType>>().Object, txRepo.Object, resRepo.Object,
            new Mock<IAuditLogService>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.GetAllHotelsForSuperAdminAsync();

        // Assert — exercises MapToSuperAdminDto with real reservation counts and revenue
        result.Should().HaveCount(1);
        result.First().TotalReservations.Should().Be(1);
        result.First().TotalRevenue.Should().Be(1000m);
    }

    // ── SuperAdminRevenueService: MapToDto with navigation properties ─────────

    [Fact]
    public async Task SuperAdminRevenueService_GetAllRevenueAsync_WithNavigationProps_MapsCorrectly()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "RES-001", UserId = Guid.NewGuid(), HotelId = hotelId, Status = ReservationStatus.Completed, TotalAmount = 1000, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow };
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", CreatedAt = DateTime.UtcNow };
        var revenue = new SuperAdminRevenue
        {
            SuperAdminRevenueId = Guid.NewGuid(), ReservationId = reservationId, HotelId = hotelId,
            ReservationAmount = 1000, CommissionAmount = 20, SuperAdminUpiId = "sa@upi",
            CreatedAt = DateTime.UtcNow,
            Reservation = reservation, // navigation property set
            Hotel = hotel              // navigation property set
        };
        var revenueRepo = new Mock<IRepository<Guid, SuperAdminRevenue>>();
        revenueRepo.Setup(r => r.GetQueryable()).Returns(new List<SuperAdminRevenue> { revenue }.AsQueryable().BuildMock());
        var sut = new SuperAdminRevenueService(revenueRepo.Object,
            new Mock<IRepository<Guid, Reservation>>().Object,
            new Mock<IRepository<Guid, Hotel>>().Object,
            new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.GetAllRevenueAsync(1, 10);

        // Assert — exercises MapToDto with non-null navigation properties (lines 95-104)
        result.TotalCount.Should().Be(1);
        result.Items.First().ReservationCode.Should().Be("RES-001");
        result.Items.First().HotelName.Should().Be("Grand Hotel");
    }

    // ── ReservationService: CancelReservationAsync — all refund tiers ─────────

    private ReservationService CreateResSut(HotelBookingContext ctx,
        Mock<IWalletService>? walletSvc = null,
        Mock<IPromoCodeService>? promoSvc = null) => new(
            new Repository<Guid, Reservation>(ctx),
            new Repository<Guid, Room>(ctx),
            new Repository<Guid, RoomType>(ctx),
            new Repository<Guid, RoomTypeInventory>(ctx),
            new Repository<Guid, RoomTypeRate>(ctx),
            new Repository<Guid, ReservationRoom>(ctx),
            new Repository<Guid, Hotel>(ctx),
            new Repository<Guid, User>(ctx),
            (walletSvc ?? new Mock<IWalletService>()).Object,
            (promoSvc ?? new Mock<IPromoCodeService>()).Object,
            new Mock<ISuperAdminRevenueService>().Object,
            new UnitOfWork(ctx));

    private static async Task<Reservation> SeedPaidReservation(
        HotelBookingContext ctx, Guid userId, int daysUntilCheckIn,
        bool isCheckedIn = false, bool cancellationFeePaid = false)
    {
        var hotelId = Guid.NewGuid();
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(daysUntilCheckIn));
        var checkOut = checkIn.AddDays(2);
        var roomTypeId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var res = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = $"RES-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            UserId = userId, HotelId = hotelId, Status = ReservationStatus.Confirmed,
            TotalAmount = 1000, FinalAmount = 1000, CheckInDate = checkIn, CheckOutDate = checkOut,
            CreatedDate = DateTime.UtcNow, IsCheckedIn = isCheckedIn,
            CancellationFeePaid = cancellationFeePaid,
            ReservationRooms = new List<ReservationRoom>
            {
                new() { ReservationRoomId = Guid.NewGuid(), RoomId = roomId, RoomTypeId = roomTypeId, PricePerNight = 500 }
            },
            Transactions = new List<Transaction>
            {
                new() { TransactionId = Guid.NewGuid(), Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow }
            }
        };
        ctx.Reservations.Add(res);
        await ctx.SaveChangesAsync();
        return res;
    }

    [Fact]
    public async Task CancelReservation_AlreadyCheckedIn_NoRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_AlreadyCheckedIn_NoRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: 1, isCheckedIn: true);
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        var result = await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — IsCheckedIn=true → no refund
        result.Should().BeTrue();
        walletSvc.Verify(w => w.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelReservation_PastCheckOut_NoRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_PastCheckOut_NoRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: -5); // past
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        var result = await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — daysUntilCheckIn < 0 → no refund
        result.Should().BeTrue();
        walletSvc.Verify(w => w.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelReservation_WithProtection_BeforeCheckInDay_FullRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_WithProtection_BeforeCheckInDay_FullRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: 3, cancellationFeePaid: true);
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — CancellationFeePaid=true, daysUntilCheckIn>0 → 100% refund
        walletSvc.Verify(w => w.CreditAsync(userId, 1000m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservation_WithProtection_OnCheckInDay_HalfRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_WithProtection_OnCheckInDay_HalfRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: 0, cancellationFeePaid: true);
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — CancellationFeePaid=true, daysUntilCheckIn==0 → 50% refund
        walletSvc.Verify(w => w.CreditAsync(userId, 500m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservation_NoProtection_7PlusDays_FullRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_NoProtection_7PlusDays_FullRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: 10);
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — no protection, 7+ days → 100% refund
        walletSvc.Verify(w => w.CreditAsync(userId, 1000m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservation_NoProtection_3To6Days_HalfRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_NoProtection_3To6Days_HalfRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: 4);
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — no protection, 3-6 days → 50% refund
        walletSvc.Verify(w => w.CreditAsync(userId, 500m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservation_NoProtection_1To2Days_QuarterRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_NoProtection_1To2Days_QuarterRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: 2);
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — no protection, 1-2 days → 25% refund
        walletSvc.Verify(w => w.CreditAsync(userId, 250m, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservation_NoProtection_SameDay_NoRefund()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CancelReservation_NoProtection_SameDay_NoRefund));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var res = await SeedPaidReservation(ctx, userId, daysUntilCheckIn: 0);
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — no protection, same day → 0% refund
        walletSvc.Verify(w => w.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelReservation_WalletPlusGateway_RefundIncludesWalletAmount()
    {
        // Arrange — guest paid ₹700 via gateway (FinalAmount) + ₹300 via wallet (WalletAmountUsed)
        using var ctx = CreateContext(nameof(CancelReservation_WalletPlusGateway_RefundIncludesWalletAmount));
        var userId = Guid.NewGuid();
        var walletSvc = new Mock<IWalletService>();
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(10));
        var res = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            ReservationCode = $"RES-WALLET",
            UserId = userId, HotelId = hotelId,
            Status = ReservationStatus.Confirmed,
            TotalAmount = 1000, FinalAmount = 700, WalletAmountUsed = 300,
            CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(2),
            CreatedDate = DateTime.UtcNow, IsCheckedIn = false, CancellationFeePaid = false,
            ReservationRooms = new List<ReservationRoom>
            {
                new() { ReservationRoomId = Guid.NewGuid(), RoomId = roomId, RoomTypeId = roomTypeId, PricePerNight = 500 }
            },
            Transactions = new List<Transaction>
            {
                new() { TransactionId = Guid.NewGuid(), Amount = 700, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow }
            }
        };
        ctx.Reservations.Add(res);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx, walletSvc);

        // Act — 10 days before check-in → 100% refund
        await sut.CancelReservationAsync(userId, res.ReservationCode, "Test");

        // Assert — refund = FinalAmount (700) + WalletAmountUsed (300) = 1000
        walletSvc.Verify(w => w.CreditAsync(userId, 1000m, It.IsAny<string>()), Times.Once);
    }

    // ── ReservationService: CalculatePricingAsync — promo + wallet branches ───

    [Fact]
    public async Task ReservationService_CreateReservation_WithPromoAndWallet_AppliesDiscountAndDeduction()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_WithPromoAndWallet_AppliesDiscountAndDeduction));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", GstPercent = 0, CreatedAt = DateTime.UtcNow };
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var room = new Room { RoomId = roomId, HotelId = hotelId, RoomTypeId = roomTypeId, RoomNumber = "101", Floor = 1, IsActive = true };
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        var checkOut = checkIn.AddDays(1);
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = checkIn, TotalInventory = 5, ReservedInventory = 0 };
        var rate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = checkIn.AddDays(-10), EndDate = checkIn.AddDays(30), Rate = 1000m };
        ctx.Hotels.Add(hotel);
        ctx.RoomTypes.Add(roomType);
        ctx.Rooms.Add(room);
        ctx.RoomTypeInventories.Add(inventory);
        ctx.RoomTypeRates.Add(rate);
        await ctx.SaveChangesAsync();

        var promoSvc = new Mock<IPromoCodeService>();
        promoSvc.Setup(p => p.ValidateAsync(userId, It.IsAny<ValidatePromoCodeDto>()))
            .ReturnsAsync(new PromoCodeValidationResultDto { IsValid = true, DiscountPercent = 10, DiscountAmount = 100 });
        var walletSvc = new Mock<IWalletService>();
        walletSvc.Setup(w => w.DeductAsync(userId, It.IsAny<decimal>(), It.IsAny<string>())).ReturnsAsync(true);
        var sut = CreateResSut(ctx, walletSvc, promoSvc);

        // Act — with promo code and wallet amount
        var result = await sut.CreateReservationAsync(userId, new CreateReservationDto
        {
            HotelId = hotelId, RoomTypeId = roomTypeId,
            CheckInDate = checkIn, CheckOutDate = checkOut,
            NumberOfRooms = 1,
            PromoCodeUsed = "PROMO-TEST",
            WalletAmountToUse = 200m
        });

        // Assert — exercises CalculatePricingAsync promo + wallet branches
        result.DiscountAmount.Should().Be(100m);
        result.WalletAmountUsed.Should().Be(200m);
    }

    [Fact]
    public async Task ReservationService_CreateReservation_WithCancellationFee_AppliesFee()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_WithCancellationFee_AppliesFee));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", GstPercent = 0, CreatedAt = DateTime.UtcNow };
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var room = new Room { RoomId = roomId, HotelId = hotelId, RoomTypeId = roomTypeId, RoomNumber = "101", Floor = 1, IsActive = true };
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        var checkOut = checkIn.AddDays(1);
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = checkIn, TotalInventory = 5, ReservedInventory = 0 };
        var rate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = checkIn.AddDays(-10), EndDate = checkIn.AddDays(30), Rate = 1000m };
        ctx.Hotels.Add(hotel);
        ctx.RoomTypes.Add(roomType);
        ctx.Rooms.Add(room);
        ctx.RoomTypeInventories.Add(inventory);
        ctx.RoomTypeRates.Add(rate);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx);

        // Act — with PayCancellationFee=true
        var result = await sut.CreateReservationAsync(userId, new CreateReservationDto
        {
            HotelId = hotelId, RoomTypeId = roomTypeId,
            CheckInDate = checkIn, CheckOutDate = checkOut,
            NumberOfRooms = 1, PayCancellationFee = true
        });

        // Assert — exercises cancellationFeeAmount branch (10% of 1000 = 100)
        result.FinalAmount.Should().Be(1100m); // 1000 + 100 fee
    }

    // ── ReservationService: GetPaymentQrAsync ─────────────────────────────────

    [Fact]
    public async Task ReservationService_GetPaymentQrAsync_ValidReservation_ReturnsQr()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_GetPaymentQrAsync_ValidReservation_ReturnsQr));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", UpiId = "hotel@upi", CreatedAt = DateTime.UtcNow };
        var res = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES-QR01",
            UserId = userId, HotelId = hotelId, Hotel = hotel,
            Status = ReservationStatus.Pending, TotalAmount = 1000, FinalAmount = 1000,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            CreatedDate = DateTime.UtcNow
        };
        ctx.Hotels.Add(hotel);
        ctx.Reservations.Add(res);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx);

        // Act
        var result = await sut.GetPaymentQrAsync(userId, res.ReservationId);

        // Assert — exercises GetPaymentQrAsync and QrCodeHelper.GenerateQrCodeBase64
        result.UpiId.Should().Be("hotel@upi");
        result.Amount.Should().Be(1000m);
        result.QrCodeBase64.Should().NotBeNullOrEmpty();
    }

    // ── ReservationService: GetAdminReservationsAsync — sort branches ─────────

    [Fact]
    public async Task ReservationService_GetAdminReservationsAsync_SortByAmount_Works()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_GetAdminReservationsAsync_SortByAmount_Works));
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId); admin.UserId = adminId;
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx);

        // Act
        var result = await sut.GetAdminReservationsAsync(adminId, null, null, 1, 10, "amount", "asc");

        // Assert
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ReservationService_GetAdminReservationsAsync_SortByGuestName_Works()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_GetAdminReservationsAsync_SortByGuestName_Works));
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId); admin.UserId = adminId;
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx);

        // Act
        var result = await sut.GetAdminReservationsAsync(adminId, null, null, 1, 10, "guestname", "desc");

        // Assert
        result.TotalCount.Should().Be(0);
    }

    // ── ReservationService: GetMyReservationsPagedAsync — search filter ───────

    [Fact]
    public async Task ReservationService_GetMyReservationsPagedAsync_WithSearch_FiltersResults()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_GetMyReservationsPagedAsync_WithSearch_FiltersResults));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", CreatedAt = DateTime.UtcNow };
        var res = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES-SEARCH01",
            UserId = userId, HotelId = hotelId, Hotel = hotel,
            Status = ReservationStatus.Confirmed, TotalAmount = 1000, FinalAmount = 1000,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            CreatedDate = DateTime.UtcNow
        };
        ctx.Hotels.Add(hotel);
        ctx.Reservations.Add(res);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx);

        // Act — search by reservation code
        var result = await sut.GetMyReservationsPagedAsync(userId, 1, 10, search: "RES-SEARCH01");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    // ── ReservationService: AssignRoomsAsync — SelectedRoomIds branch ─────────

    [Fact]
    public async Task ReservationService_CreateReservation_WithSelectedRoomIds_AssignsSpecificRooms()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_WithSelectedRoomIds_AssignsSpecificRooms));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", GstPercent = 0, CreatedAt = DateTime.UtcNow };
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var room = new Room { RoomId = roomId, HotelId = hotelId, RoomTypeId = roomTypeId, RoomNumber = "101", Floor = 1, IsActive = true };
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        var checkOut = checkIn.AddDays(1);
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = checkIn, TotalInventory = 5, ReservedInventory = 0 };
        var rate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = checkIn.AddDays(-10), EndDate = checkIn.AddDays(30), Rate = 1000m };
        ctx.Hotels.Add(hotel); ctx.RoomTypes.Add(roomType); ctx.Rooms.Add(room);
        ctx.RoomTypeInventories.Add(inventory); ctx.RoomTypeRates.Add(rate);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx);

        // Act — with explicit SelectedRoomIds
        var result = await sut.CreateReservationAsync(userId, new CreateReservationDto
        {
            HotelId = hotelId, RoomTypeId = roomTypeId,
            CheckInDate = checkIn, CheckOutDate = checkOut,
            NumberOfRooms = 1, SelectedRoomIds = new List<Guid> { roomId }
        });

        // Assert — exercises SelectedRoomIds branch in AssignRoomsAsync
        result.TotalRooms.Should().Be(1);
        result.Rooms.First().RoomId.Should().Be(roomId);
    }

    [Fact]
    public async Task ReservationService_CreateReservation_SelectedRoomCountMismatch_ThrowsValidationException()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_SelectedRoomCountMismatch_ThrowsValidationException));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", GstPercent = 0, CreatedAt = DateTime.UtcNow };
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        var checkOut = checkIn.AddDays(1);
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = checkIn, TotalInventory = 5, ReservedInventory = 0 };
        var rate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = checkIn.AddDays(-10), EndDate = checkIn.AddDays(30), Rate = 1000m };
        ctx.Hotels.Add(hotel); ctx.RoomTypes.Add(roomType);
        ctx.RoomTypeInventories.Add(inventory); ctx.RoomTypeRates.Add(rate);
        await ctx.SaveChangesAsync();
        var sut = CreateResSut(ctx);

        // Act — 2 rooms requested but only 1 selected room ID
        var act = async () => await sut.CreateReservationAsync(userId, new CreateReservationDto
        {
            HotelId = hotelId, RoomTypeId = roomTypeId,
            CheckInDate = checkIn, CheckOutDate = checkOut,
            NumberOfRooms = 2, SelectedRoomIds = new List<Guid> { Guid.NewGuid() }
        });

        // Assert — exercises "Selected room count must match" branch
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*count*");
    }

    // ── ReservationService: ProcessWalletDeductionAsync — insufficient wallet ─

    [Fact]
    public async Task ReservationService_CreateReservation_InsufficientWallet_ThrowsValidationException()
    {
        // Arrange
        using var ctx = CreateContext(nameof(ReservationService_CreateReservation_InsufficientWallet_ThrowsValidationException));
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var hotel = new Hotel { HotelId = hotelId, Name = "Grand Hotel", Address = "A", City = "C", ContactNumber = "9999999999", GstPercent = 0, CreatedAt = DateTime.UtcNow };
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var room = new Room { RoomId = roomId, HotelId = hotelId, RoomTypeId = roomTypeId, RoomNumber = "101", Floor = 1, IsActive = true };
        var checkIn = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        var checkOut = checkIn.AddDays(1);
        var inventory = new RoomTypeInventory { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = checkIn, TotalInventory = 5, ReservedInventory = 0 };
        var rate = new RoomTypeRate { RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId, StartDate = checkIn.AddDays(-10), EndDate = checkIn.AddDays(30), Rate = 1000m };
        ctx.Hotels.Add(hotel); ctx.RoomTypes.Add(roomType); ctx.Rooms.Add(room);
        ctx.RoomTypeInventories.Add(inventory); ctx.RoomTypeRates.Add(rate);
        await ctx.SaveChangesAsync();
        var walletSvc = new Mock<IWalletService>();
        walletSvc.Setup(w => w.DeductAsync(userId, It.IsAny<decimal>(), It.IsAny<string>())).ReturnsAsync(false); // insufficient
        var sut = CreateResSut(ctx, walletSvc);

        // Act
        var act = async () => await sut.CreateReservationAsync(userId, new CreateReservationDto
        {
            HotelId = hotelId, RoomTypeId = roomTypeId,
            CheckInDate = checkIn, CheckOutDate = checkOut,
            NumberOfRooms = 1, WalletAmountToUse = 500m
        });

        // Assert — exercises ProcessWalletDeductionAsync "Insufficient wallet balance" branch
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*wallet*");
    }
}
