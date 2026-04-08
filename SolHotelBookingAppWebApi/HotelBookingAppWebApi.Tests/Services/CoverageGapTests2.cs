using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using HotelBookingAppWebApi.Repository;
using HotelBookingAppWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

/// <summary>
/// Fills remaining coverage gaps to achieve 100% line and branch coverage.
/// Uses AAA pattern throughout.
/// </summary>
public class CoverageGapTests2
{
    // ── Shared helpers ────────────────────────────────────────────────────────

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

    private static Hotel MakeHotel(Guid? hotelId = null) => new()
    {
        HotelId = hotelId ?? Guid.NewGuid(), Name = "Grand Hotel", Address = "123 Main",
        City = "Mumbai", State = "MH", ContactNumber = "9999999999",
        IsActive = true, CreatedAt = DateTime.UtcNow,
        RoomTypes = new List<RoomType>(), Reviews = new List<Review>()
    };

    // ── AuditLogService — branch coverage ─────────────────────────────────────

    [Fact]
    public async Task AuditLogService_GetAdminAuditLogsAsync_WithSearch_FiltersResults()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var users = new List<User> { new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock();
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetQueryable()).Returns(users);
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = Guid.NewGuid(), UserId = adminId, Action = "UpdateHotel", EntityName = "Hotel", Changes = "{}", CreatedAt = DateTime.UtcNow },
            new() { AuditLogId = Guid.NewGuid(), UserId = adminId, Action = "DeleteRoom", EntityName = "Room", Changes = "{}", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var auditRepo = new Mock<IRepository<Guid, AuditLog>>();
        auditRepo.Setup(r => r.GetQueryable()).Returns(logs);
        var uow = new Mock<IUnitOfWork>();
        var sut = new AuditLogService(auditRepo.Object, userRepo.Object, uow.Object);

        // Act
        var result = await sut.GetAdminAuditLogsAsync(adminId, 1, 10, "UpdateHotel");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task AuditLogService_GetAllAuditLogsAsync_WithAllFilters_FiltersCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = Guid.NewGuid(), UserId = userId, EntityId = hotelId, Action = "Create", EntityName = "Hotel", Changes = "{}", CreatedAt = now },
            new() { AuditLogId = Guid.NewGuid(), UserId = Guid.NewGuid(), EntityId = Guid.NewGuid(), Action = "Delete", EntityName = "Room", Changes = "{}", CreatedAt = now.AddDays(-5) }
        }.AsQueryable().BuildMock();
        var auditRepo = new Mock<IRepository<Guid, AuditLog>>();
        auditRepo.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = new AuditLogService(auditRepo.Object, new Mock<IRepository<Guid, User>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.GetAllAuditLogsAsync(1, 10, hotelId: hotelId, userId: userId, action: "Create", dateFrom: now.AddDays(-1), dateTo: now.AddDays(1));

        // Assert
        result.TotalCount.Should().Be(1);
    }

    // ── DashboardService — branch coverage ────────────────────────────────────

    [Fact]
    public async Task DashboardService_GetAdminDashboardAsync_HotelNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var users = new List<User> { new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock();
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetQueryable()).Returns(users);
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync((Hotel?)null);
        var sut = new DashboardService(userRepo.Object, hotelRepo.Object,
            new Mock<IRepository<Guid, Reservation>>().Object,
            new Mock<IRepository<Guid, Transaction>>().Object,
            new Mock<IRepository<Guid, Review>>().Object,
            new Mock<IRepository<Guid, Room>>().Object,
            new Mock<IRepository<Guid, RoomType>>().Object,
            new Mock<IRepository<Guid, SuperAdminRevenue>>().Object);

        // Act
        var act = async () => await sut.GetAdminDashboardAsync(adminId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DashboardService_GetAdminDashboardAsync_WithReviews_CalculatesAverage()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var users = new List<User> { new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock();
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetQueryable()).Returns(users);
        var hotel = MakeHotel(hotelId);
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var reviews = new List<Review>
        {
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Good", CreatedDate = DateTime.UtcNow },
            new() { ReviewId = Guid.NewGuid(), HotelId = hotelId, UserId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 5, Comment = "Great", CreatedDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var reviewRepo = new Mock<IRepository<Guid, Review>>();
        reviewRepo.Setup(r => r.GetQueryable()).Returns(reviews);
        var roomRepo = new Mock<IRepository<Guid, Room>>();
        roomRepo.Setup(r => r.GetQueryable()).Returns(new List<Room>().AsQueryable().BuildMock());
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        roomTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<RoomType>().AsQueryable().BuildMock());
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var txRepo = new Mock<IRepository<Guid, Transaction>>();
        txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        var sut = new DashboardService(userRepo.Object, hotelRepo.Object, resRepo.Object, txRepo.Object, reviewRepo.Object, roomRepo.Object, roomTypeRepo.Object, new Mock<IRepository<Guid, SuperAdminRevenue>>().Object);

        // Act
        var result = await sut.GetAdminDashboardAsync(adminId);

        // Assert
        result.TotalReviews.Should().Be(2);
        result.AverageRating.Should().Be(4.5m);
    }

    // ── HotelService — missing branches ───────────────────────────────────────

    private HotelService CreateHotelSut(
        Mock<IRepository<Guid, Hotel>> hotelRepo,
        Mock<IRepository<Guid, User>>? userRepo = null,
        Mock<IRepository<Guid, RoomType>>? roomTypeRepo = null,
        Mock<IRepository<Guid, Transaction>>? txRepo = null,
        Mock<IRepository<Guid, Reservation>>? resRepo = null) => new(
            hotelRepo.Object,
            (userRepo ?? new Mock<IRepository<Guid, User>>()).Object,
            (roomTypeRepo ?? new Mock<IRepository<Guid, RoomType>>()).Object,
            (txRepo ?? new Mock<IRepository<Guid, Transaction>>()).Object,
            (resRepo ?? new Mock<IRepository<Guid, Reservation>>()).Object,
            new Mock<IAuditLogService>().Object,
            new Mock<IUnitOfWork>().Object);

    [Fact]
    public async Task HotelService_GetActiveStatesAsync_ReturnsDistinctStates()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel() }.AsQueryable().BuildMock();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateHotelSut(hotelRepo);

        // Act
        var result = await sut.GetActiveStatesAsync();

        // Assert
        result.Should().Contain("MH");
    }

    [Fact]
    public async Task HotelService_GetHotelsByStateAsync_ValidState_ReturnsHotels()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotels = new List<Hotel> { MakeHotel() }.AsQueryable().BuildMock();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateHotelSut(hotelRepo);

        // Act
        var result = await sut.GetHotelsByStateAsync("MH");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task HotelService_GetRoomTypesAsync_ValidHotel_ReturnsRoomTypes()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        var roomTypes = new List<RoomType>
        {
            new() { RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true, RoomTypeAmenities = new List<RoomTypeAmenity>(), Rates = new List<RoomTypeRate>() }
        }.AsQueryable().BuildMock();
        roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);
        var sut = CreateHotelSut(hotelRepo, roomTypeRepo: roomTypeRepo);

        // Act
        var result = await sut.GetRoomTypesAsync(hotelId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task HotelService_GetAllHotelsForSuperAdminAsync_ReturnsAll()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var txRepo = new Mock<IRepository<Guid, Transaction>>();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel() }.AsQueryable().BuildMock();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);
        txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, txRepo: txRepo, resRepo: resRepo);

        // Act
        var result = await sut.GetAllHotelsForSuperAdminAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task HotelService_GetAllHotelsForSuperAdminPagedAsync_WithSearchAndStatus_FiltersCorrectly()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var txRepo = new Mock<IRepository<Guid, Transaction>>();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var active = MakeHotel(); active.IsActive = true;
        var inactive = MakeHotel(); inactive.IsActive = false;
        var hotels = new List<Hotel> { active, inactive }.AsQueryable().BuildMock();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);
        txRepo.Setup(r => r.GetQueryable()).Returns(new List<Transaction>().AsQueryable().BuildMock());
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, txRepo: txRepo, resRepo: resRepo);

        // Act
        var result = await sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, search: "Grand", status: "Active");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HotelService_UpdateHotelAsync_AdminNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);
        var sut = CreateHotelSut(hotelRepo, userRepo);

        // Act
        var act = async () => await sut.UpdateHotelAsync(Guid.NewGuid(), new UpdateHotelDto { Name = "X", Address = "Y", City = "Z", ContactNumber = "1234567890" });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task HotelService_ToggleHotelStatusAsync_Activate_SetsActive()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var hotel = MakeHotel(hotelId); hotel.IsActive = false;
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var uow = new Mock<IUnitOfWork>();
        var sut = new HotelService(hotelRepo.Object, userRepo.Object,
            new Mock<IRepository<Guid, RoomType>>().Object,
            new Mock<IRepository<Guid, Transaction>>().Object,
            new Mock<IRepository<Guid, Reservation>>().Object,
            new Mock<IAuditLogService>().Object, uow.Object);

        // Act
        await sut.ToggleHotelStatusAsync(admin.UserId, true);

        // Assert
        hotel.IsActive.Should().BeTrue();
        uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    // ── AmenityRequestService — missing branches ──────────────────────────────

    private AmenityRequestService CreateAmenityRequestSut(
        Mock<IRepository<Guid, AmenityRequest>> requestRepo,
        Mock<IRepository<Guid, Amenity>>? amenityRepo = null,
        Mock<IRepository<Guid, User>>? userRepo = null,
        Mock<IRepository<Guid, Hotel>>? hotelRepo = null,
        Mock<IUnitOfWork>? uow = null) => new(
            requestRepo.Object,
            (amenityRepo ?? new Mock<IRepository<Guid, Amenity>>()).Object,
            (userRepo ?? new Mock<IRepository<Guid, User>>()).Object,
            (hotelRepo ?? new Mock<IRepository<Guid, Hotel>>()).Object,
            (uow ?? new Mock<IUnitOfWork>()).Object);

    [Fact]
    public async Task AmenityRequestService_GetAdminRequestsAsync_ValidAdmin_ReturnsList()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        admin.UserId = adminId;
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var hotel = MakeHotel(hotelId);
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Sauna", Category = "Wellness", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var requestRepo = new Mock<IRepository<Guid, AmenityRequest>>();
        requestRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateAmenityRequestSut(requestRepo, userRepo: userRepo, hotelRepo: hotelRepo);

        // Act
        var result = await sut.GetAdminRequestsAsync(adminId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task AmenityRequestService_GetAdminRequestsPagedAsync_WithSearch_FiltersResults()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId); admin.UserId = adminId;
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var hotel = MakeHotel(hotelId);
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Sauna", Category = "Wellness", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow },
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Pool", Category = "Recreation", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var requestRepo = new Mock<IRepository<Guid, AmenityRequest>>();
        requestRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateAmenityRequestSut(requestRepo, userRepo: userRepo, hotelRepo: hotelRepo);

        // Act
        var result = await sut.GetAdminRequestsPagedAsync(adminId, 1, 10, search: "Sauna");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task AmenityRequestService_GetAllRequestsAsync_NoStatusFilter_ReturnsAll()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Sauna", Category = "Wellness", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow },
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Pool", Category = "Recreation", Status = AmenityRequestStatus.Approved, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var requestRepo = new Mock<IRepository<Guid, AmenityRequest>>();
        requestRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(new List<Hotel> { MakeHotel(hotelId) }.AsQueryable().BuildMock());
        var sut = CreateAmenityRequestSut(requestRepo, hotelRepo: hotelRepo);

        // Act
        var result = await sut.GetAllRequestsAsync(null, 1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task AmenityRequestService_RejectRequestAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var requestRepo = new Mock<IRepository<Guid, AmenityRequest>>();
        requestRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((AmenityRequest?)null);
        var sut = CreateAmenityRequestSut(requestRepo);

        // Act
        var act = async () => await sut.RejectRequestAsync(Guid.NewGuid(), Guid.NewGuid(), "Not needed.");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AmenityRequestService_CreateRequestAsync_AdminHasNoHotel_ThrowsValidationException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = new User { UserId = adminId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow };
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var requestRepo = new Mock<IRepository<Guid, AmenityRequest>>();
        var sut = CreateAmenityRequestSut(requestRepo, userRepo: userRepo);

        // Act
        var act = async () => await sut.CreateRequestAsync(adminId, new CreateAmenityRequestDto { AmenityName = "Sauna", Category = "Wellness" });

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── WalletService — missing branches ──────────────────────────────────────

    [Fact]
    public async Task WalletService_GetWalletAsync_CreatesWalletIfMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        var walletTxRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var uow = new Mock<IUnitOfWork>();
        // First call returns null (no wallet), second call returns the created wallet
        var newWallet = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 0, UpdatedAt = DateTime.UtcNow };
        walletRepo.SetupSequence(r => r.GetQueryable())
            .Returns(new List<Wallet>().AsQueryable().BuildMock())
            .Returns(new List<Wallet> { newWallet }.AsQueryable().BuildMock());
        walletRepo.Setup(r => r.AddAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet w) => w);
        walletTxRepo.Setup(r => r.GetQueryable()).Returns(new List<WalletTransaction>().AsQueryable().BuildMock());
        var sut = new WalletService(walletRepo.Object, walletTxRepo.Object, userRepo.Object, uow.Object);

        // Act
        var result = await sut.GetWalletAsync(userId, 1, 10);

        // Assert
        walletRepo.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Once);
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task WalletService_DebitAsync_ZeroBalance_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 0, UpdatedAt = DateTime.UtcNow };
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        var sut = new WalletService(walletRepo.Object, new Mock<IRepository<Guid, WalletTransaction>>().Object, new Mock<IRepository<Guid, User>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.DebitAsync(userId, 100m, "Payment");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WalletService_DebitAsync_PartialBalance_DebitsAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 50m, UpdatedAt = DateTime.UtcNow };
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        walletRepo.Setup(r => r.GetQueryable()).Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        var walletTxRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        walletTxRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync((WalletTransaction wt) => wt);
        var sut = new WalletService(walletRepo.Object, walletTxRepo.Object, new Mock<IRepository<Guid, User>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.DebitAsync(userId, 100m, "Partial debit");

        // Assert
        result.Should().BeTrue();
        wallet.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task WalletService_TopUpAsync_NegativeAmount_ThrowsValidationException()
    {
        // Arrange
        var sut = new WalletService(new Mock<IRepository<Guid, Wallet>>().Object, new Mock<IRepository<Guid, WalletTransaction>>().Object, new Mock<IRepository<Guid, User>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = async () => await sut.TopUpAsync(Guid.NewGuid(), -50m);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task WalletService_GetGuestWalletByAdminAsync_AdminNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync((User?)null);
        var sut = new WalletService(new Mock<IRepository<Guid, Wallet>>().Object, new Mock<IRepository<Guid, WalletTransaction>>().Object, userRepo.Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = async () => await sut.GetGuestWalletByAdminAsync(adminId, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── PromoCodeService — CalculateDiscountPercent branches ─────────────────

    [Fact]
    public async Task PromoCodeService_GeneratePromo_Amount500_Gets5Percent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "R1", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 400, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow };
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        promoRepo.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).ReturnsAsync((PromoCode p) => p);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(reservationId);

        // Assert — 5% for <= 500
        promoRepo.Verify(r => r.AddAsync(It.Is<PromoCode>(p => p.DiscountPercent == 5)), Times.Once);
    }

    [Fact]
    public async Task PromoCodeService_GeneratePromo_Amount1000_Gets10Percent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "R1", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 800, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow };
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        promoRepo.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).ReturnsAsync((PromoCode p) => p);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(reservationId);

        // Assert — 10% for <= 1000
        promoRepo.Verify(r => r.AddAsync(It.Is<PromoCode>(p => p.DiscountPercent == 10)), Times.Once);
    }

    [Fact]
    public async Task PromoCodeService_GeneratePromo_Amount2000_Gets15Percent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "R1", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 1500, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow };
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        promoRepo.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).ReturnsAsync((PromoCode p) => p);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(reservationId);

        // Assert — 15% for <= 2000
        promoRepo.Verify(r => r.AddAsync(It.Is<PromoCode>(p => p.DiscountPercent == 15)), Times.Once);
    }

    [Fact]
    public async Task PromoCodeService_GeneratePromo_Amount5000_Gets20Percent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "R1", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 3000, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow };
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        promoRepo.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).ReturnsAsync((PromoCode p) => p);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(reservationId);

        // Assert — 20% for <= 5000
        promoRepo.Verify(r => r.AddAsync(It.Is<PromoCode>(p => p.DiscountPercent == 20)), Times.Once);
    }

    [Fact]
    public async Task PromoCodeService_GeneratePromo_AmountOver5000_Gets25Percent()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "R1", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 10000, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow };
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        promoRepo.Setup(r => r.AddAsync(It.IsAny<PromoCode>())).ReturnsAsync((PromoCode p) => p);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(reservationId);

        // Assert — 25% for > 5000
        promoRepo.Verify(r => r.AddAsync(It.Is<PromoCode>(p => p.DiscountPercent == 25)), Times.Once);
    }

    [Fact]
    public async Task PromoCodeService_GeneratePromo_ReservationNotFound_DoesNothing()
    {
        // Arrange
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation>().AsQueryable().BuildMock());
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(Guid.NewGuid());

        // Assert
        promoRepo.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Never);
    }

    [Fact]
    public async Task PromoCodeService_GeneratePromo_AlreadyExists_DoesNothing()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var reservation = new Reservation { ReservationId = reservationId, ReservationCode = "R1", UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(), TotalAmount = 1000, Status = ReservationStatus.Completed, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CreatedDate = DateTime.UtcNow };
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(new List<Reservation> { reservation }.AsQueryable().BuildMock());
        var existingPromo = new PromoCode { PromoCodeId = Guid.NewGuid(), Code = "PROMO-EXIST", UserId = reservation.UserId, HotelId = reservation.HotelId, ReservationId = reservationId, DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(90), IsUsed = false, CreatedAt = DateTime.UtcNow };
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(new List<PromoCode> { existingPromo }.AsQueryable().BuildMock());
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.GeneratePromoForCompletedReservationAsync(reservationId);

        // Assert
        promoRepo.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Never);
    }

    [Fact]
    public async Task PromoCodeService_MarkUsedAsync_PromoNotFound_DoesNothing()
    {
        // Arrange
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(new List<PromoCode>().AsQueryable().BuildMock());
        var sut = new PromoCodeService(promoRepo.Object, new Mock<IRepository<Guid, Reservation>>().Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        await sut.MarkUsedAsync("NONEXISTENT", Guid.NewGuid());

        // Assert — no exception, no save
        promoRepo.Verify(r => r.AddAsync(It.IsAny<PromoCode>()), Times.Never);
    }

    [Fact]
    public async Task PromoCodeService_GetGuestPromoCodesPagedAsync_AllFilter_ReturnsAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            new() { PromoCodeId = Guid.NewGuid(), Code = "P1", UserId = userId, HotelId = hotelId, DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(30), IsUsed = false, CreatedAt = DateTime.UtcNow },
            new() { PromoCodeId = Guid.NewGuid(), Code = "P2", UserId = userId, HotelId = hotelId, DiscountPercent = 15, ExpiryDate = DateTime.UtcNow.AddDays(-5), IsUsed = true, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        promoRepo.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = new PromoCodeService(promoRepo.Object, new Mock<IRepository<Guid, Reservation>>().Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.GetGuestPromoCodesPagedAsync(userId, 1, 10, "All");

        // Assert
        result.TotalCount.Should().Be(2);
    }

    // ── InventoryService — missing branches ───────────────────────────────────

    [Fact]
    public async Task InventoryService_AddInventoryAsync_SkipsExistingDates()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        roomTypeRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomType, bool>>>())).ReturnsAsync(roomType);
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Seed one existing date
        var existingInventory = new List<RoomTypeInventory>
        {
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = today, TotalInventory = 5, ReservedInventory = 0 }
        }.AsQueryable().BuildMock();
        var inventoryRepo = new Mock<IRepository<Guid, RoomTypeInventory>>();
        inventoryRepo.Setup(r => r.GetQueryable()).Returns(existingInventory);
        inventoryRepo.Setup(r => r.AddAsync(It.IsAny<RoomTypeInventory>())).ReturnsAsync((RoomTypeInventory i) => i);
        var uow = new Mock<IUnitOfWork>();
        var sut = new InventoryService(inventoryRepo.Object, roomTypeRepo.Object, userRepo.Object, uow.Object);
        var dto = new CreateInventoryDto { RoomTypeId = roomTypeId, StartDate = today, EndDate = today.AddDays(1), TotalInventory = 5 };

        // Act
        await sut.AddInventoryAsync(admin.UserId, dto);

        // Assert — only 1 new date added (today was skipped)
        inventoryRepo.Verify(r => r.AddAsync(It.IsAny<RoomTypeInventory>()), Times.Once);
    }

    [Fact]
    public async Task InventoryService_AddInventoryAsync_RoomTypeNotOwned_ThrowsNotFoundException()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        roomTypeRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RoomType, bool>>>())).ReturnsAsync((RoomType?)null);
        var inventoryRepo = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new InventoryService(inventoryRepo.Object, roomTypeRepo.Object, userRepo.Object, uow.Object);

        // Act
        var act = async () => await sut.AddInventoryAsync(admin.UserId, new CreateInventoryDto { RoomTypeId = Guid.NewGuid(), StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), TotalInventory = 5 });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── UserService — missing branches ────────────────────────────────────────

    [Fact]
    public async Task UserService_GetProfileAsync_UserWithNoDetails_AutoCreatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Name = "Bob", Email = "bob@test.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow, UserDetails = null };
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetQueryable()).Returns(new List<User> { user }.AsQueryable().BuildMock());
        var reviewRepo = new Mock<IRepository<Guid, Review>>();
        reviewRepo.Setup(r => r.GetQueryable()).Returns(new List<Review>().AsQueryable().BuildMock());
        var uow = new Mock<IUnitOfWork>();
        var sut = new UserService(userRepo.Object, new Mock<IRepository<Guid, Reservation>>().Object, reviewRepo.Object, uow.Object);

        // Act
        var result = await sut.GetProfileAsync(userId);

        // Assert — profile auto-created
        result.Email.Should().Be("bob@test.com");
        uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UserService_UpdateProfileAsync_AllFieldsNull_NoChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId, Name = "Alice", Email = "alice@test.com",
            Password = new byte[]{1}, PasswordSaltValue = new byte[]{2},
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow,
            UserDetails = new UserProfileDetails { UserDetailsId = Guid.NewGuid(), UserId = userId, Name = "Alice", Email = "alice@test.com", PhoneNumber = "9999", Address = "Addr", State = "MH", City = "Mumbai", Pincode = "400001", CreatedAt = DateTime.UtcNow }
        };
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetQueryable()).Returns(new List<User> { user }.AsQueryable().BuildMock());
        var uow = new Mock<IUnitOfWork>();
        var sut = new UserService(userRepo.Object, new Mock<IRepository<Guid, Reservation>>().Object, new Mock<IRepository<Guid, Review>>().Object, uow.Object);

        // Act — pass all nulls/empty
        var result = await sut.UpdateProfileAsync(userId, new UpdateUserProfileDto());

        // Assert — no changes, original values preserved
        result.Name.Should().Be("Alice");
    }

    // ── SupportRequestService — missing branches ──────────────────────────────

    private SupportRequestService CreateSupportSut(
        Mock<IRepository<Guid, SupportRequest>>? supportRepo = null,
        Mock<IRepository<Guid, User>>? userRepo = null,
        Mock<IRepository<Guid, Hotel>>? hotelRepo = null,
        Mock<IUnitOfWork>? uow = null) => new(
            (supportRepo ?? new Mock<IRepository<Guid, SupportRequest>>()).Object,
            (userRepo ?? new Mock<IRepository<Guid, User>>()).Object,
            (hotelRepo ?? new Mock<IRepository<Guid, Hotel>>()).Object,
            (uow ?? new Mock<IUnitOfWork>()).Object);

    [Fact]
    public async Task SupportRequestService_CreatePublicRequestAsync_ReturnsDto()
    {
        // Arrange
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        supportRepo.Setup(r => r.AddAsync(It.IsAny<SupportRequest>())).ReturnsAsync((SupportRequest sr) => sr);
        var sut = CreateSupportSut(supportRepo);

        // Act
        var result = await sut.CreatePublicRequestAsync(new PublicSupportRequestDto { Name = "John", Email = "john@test.com", Subject = "Issue", Message = "Help me", Category = "General" });

        // Assert
        result.Subject.Should().Be("Issue");
    }

    [Fact]
    public async Task SupportRequestService_CreateAdminRequestAsync_ValidAdmin_ReturnsDto()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = MakeAdmin();
        admin.UserId = adminId;
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        supportRepo.Setup(r => r.AddAsync(It.IsAny<SupportRequest>())).ReturnsAsync((SupportRequest sr) => sr);
        var sut = CreateSupportSut(supportRepo, userRepo);

        // Act
        var result = await sut.CreateAdminRequestAsync(adminId, new AdminSupportRequestDto { Subject = "Billing", Message = "Overcharged", Category = "Finance" });

        // Assert
        result.Subject.Should().Be("Billing");
    }

    [Fact]
    public async Task SupportRequestService_GetGuestRequestsAsync_ValidGuest_ReturnsPaged()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { UserId = userId, Name = "Guest", Email = "g@test.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow };
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(userId)).ReturnsAsync(user);
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), UserId = userId, Subject = "Issue", Message = "Help", Category = "General", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        supportRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSupportSut(supportRepo, userRepo);

        // Act
        var result = await sut.GetGuestRequestsAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SupportRequestService_GetAdminRequestsAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = MakeAdmin(); admin.UserId = adminId;
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), UserId = adminId, Subject = "Issue", Message = "Help", Category = "General", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        supportRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSupportSut(supportRepo, userRepo);

        // Act
        var result = await sut.GetAdminRequestsAsync(adminId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SupportRequestService_GetAllRequestsAsync_WithFilters_FiltersCorrectly()
    {
        // Arrange
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), Subject = "Open Issue", Message = "Help", Category = "General", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow },
            new() { SupportRequestId = Guid.NewGuid(), Subject = "Resolved Issue", Message = "Fixed", Category = "Technical", Status = SupportRequestStatus.Resolved, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        supportRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSupportSut(supportRepo);

        // Act
        var result = await sut.GetAllRequestsAsync("Open", null, "Open Issue", 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SupportRequestService_RespondAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        supportRepo.Setup(r => r.GetQueryable()).Returns(new List<SupportRequest>().AsQueryable().BuildMock());
        var sut = CreateSupportSut(supportRepo);

        // Act
        var act = async () => await sut.RespondAsync(Guid.NewGuid(), new RespondSupportRequestDto { Response = "Fixed" });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SupportRequestService_CreateGuestRequestAsync_UserNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        var userRepo = new Mock<IRepository<Guid, User>>();
        userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var sut = CreateSupportSut(userRepo: userRepo);

        // Act
        var act = async () => await sut.CreateGuestRequestAsync(Guid.NewGuid(), new GuestSupportRequestDto { Subject = "Issue", Message = "Help", Category = "General" });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── SuperAdminRevenueService — GetAllRevenueAsync with empty result ────────

    [Fact]
    public async Task SuperAdminRevenueService_GetAllRevenueAsync_EmptyResult_ReturnsEmpty()
    {
        // Arrange
        var revenueRepo = new Mock<IRepository<Guid, SuperAdminRevenue>>();
        revenueRepo.Setup(r => r.GetQueryable()).Returns(new List<SuperAdminRevenue>().AsQueryable().BuildMock());
        var sut = new SuperAdminRevenueService(revenueRepo.Object, new Mock<IRepository<Guid, Reservation>>().Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.GetAllRevenueAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SuperAdminRevenueService_GetSummaryAsync_EmptyRevenue_ReturnsZero()
    {
        // Arrange
        var revenueRepo = new Mock<IRepository<Guid, SuperAdminRevenue>>();
        revenueRepo.Setup(r => r.GetQueryable()).Returns(new List<SuperAdminRevenue>().AsQueryable().BuildMock());
        var sut = new SuperAdminRevenueService(revenueRepo.Object, new Mock<IRepository<Guid, Reservation>>().Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var result = await sut.GetSummaryAsync();

        // Assert
        result.TotalCommissionEarned.Should().Be(0m);
    }

    // ── AuthService — missing branches ────────────────────────────────────────

    [Fact]
    public async Task AuthService_RegisterGuestAsync_ExceptionDuringCreate_RollsBack()
    {
        // Arrange
        var userRepo = new Mock<IRepository<Guid, User>>();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var profileRepo = new Mock<IRepository<Guid, UserProfileDetails>>();
        var passwordSvc = new Mock<IPasswordService>();
        var tokenSvc = new Mock<ITokenService>();
        var walletSvc = new Mock<IWalletService>();
        var uow = new Mock<IUnitOfWork>();
        // Email is unique
        userRepo.Setup(r => r.GetQueryable()).Returns(new List<User>().AsQueryable().BuildMock());
        // Simulate failure during user creation
        userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ThrowsAsync(new Exception("DB error"));
        passwordSvc.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]?>(), out It.Ref<byte[]?>.IsAny)).Returns(new byte[] { 1, 2, 3 });
        var sut = new AuthService(userRepo.Object, hotelRepo.Object, profileRepo.Object, passwordSvc.Object, tokenSvc.Object, walletSvc.Object, uow.Object);

        // Act
        var act = async () => await sut.RegisterGuestAsync(new HotelBookingAppWebApi.Models.DTOs.Auth.RegisterUserDto { Name = "Test", Email = "test@test.com", Password = "pass123" });

        // Assert
        await act.Should().ThrowAsync<Exception>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AuthService_RegisterHotelAdminAsync_ExceptionDuringCreate_RollsBack()
    {
        // Arrange
        var userRepo = new Mock<IRepository<Guid, User>>();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var profileRepo = new Mock<IRepository<Guid, UserProfileDetails>>();
        var passwordSvc = new Mock<IPasswordService>();
        var tokenSvc = new Mock<ITokenService>();
        var walletSvc = new Mock<IWalletService>();
        var uow = new Mock<IUnitOfWork>();
        userRepo.Setup(r => r.GetQueryable()).Returns(new List<User>().AsQueryable().BuildMock());
        hotelRepo.Setup(r => r.AddAsync(It.IsAny<Hotel>())).ThrowsAsync(new Exception("DB error"));
        passwordSvc.Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]?>(), out It.Ref<byte[]?>.IsAny)).Returns(new byte[] { 1, 2, 3 });
        var sut = new AuthService(userRepo.Object, hotelRepo.Object, profileRepo.Object, passwordSvc.Object, tokenSvc.Object, walletSvc.Object, uow.Object);

        // Act
        var act = async () => await sut.RegisterHotelAdminAsync(new HotelBookingAppWebApi.Models.DTOs.Auth.RegisterHotelAdminDto { Name = "Admin", Email = "admin@test.com", Password = "pass123", HotelName = "Hotel", Address = "Addr", City = "City", State = "State", ContactNumber = "1234567890" });

        // Assert
        await act.Should().ThrowAsync<Exception>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AuthService_LoginAsync_InactiveUser_ThrowsUnAuthorizedException()
    {
        // Arrange
        var userRepo = new Mock<IRepository<Guid, User>>();
        var inactiveUser = new User { UserId = Guid.NewGuid(), Name = "Inactive", Email = "inactive@test.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Guest, IsActive = false, CreatedAt = DateTime.UtcNow };
        userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(inactiveUser);
        var sut = new AuthService(userRepo.Object, new Mock<IRepository<Guid, Hotel>>().Object, new Mock<IRepository<Guid, UserProfileDetails>>().Object, new Mock<IPasswordService>().Object, new Mock<ITokenService>().Object, new Mock<IWalletService>().Object, new Mock<IUnitOfWork>().Object);

        // Act
        var act = async () => await sut.LoginAsync(new HotelBookingAppWebApi.Models.DTOs.Auth.LoginDto { Email = "inactive@test.com", Password = "pass" });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>().WithMessage("*deactivated*");
    }

    // ── RoomTypeService — missing branches (using in-memory DB) ───────────────

    private RoomTypeService CreateRoomTypeSut(string dbName, HotelBookingContext? ctx = null)
    {
        var context = ctx ?? CreateContext(dbName);
        return new RoomTypeService(
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomType>(context),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeRate>(context),
            new HotelBookingAppWebApi.Repository.Repository<Guid, RoomTypeAmenity>(context),
            new HotelBookingAppWebApi.Repository.Repository<Guid, User>(context),
            new Mock<IAuditLogService>().Object,
            new HotelBookingAppWebApi.Services.UnitOfWork(context),
            context);
    }

    [Fact]
    public async Task RoomTypeService_AddRoomTypeAsync_ValidDto_AddsRoomType()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RoomTypeService_AddRoomTypeAsync_ValidDto_AddsRoomType));
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();
        var sut = CreateRoomTypeSut(nameof(RoomTypeService_AddRoomTypeAsync_ValidDto_AddsRoomType), ctx);
        var dto = new CreateRoomTypeDto { Name = "Deluxe", Description = "Nice room", MaxOccupancy = 2, AmenityIds = new List<Guid>() };

        // Act
        await sut.AddRoomTypeAsync(admin.UserId, dto);

        // Assert
        ctx.RoomTypes.Should().HaveCount(1);
    }

    [Fact]
    public async Task RoomTypeService_AddRoomTypeAsync_AdminHasNoHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RoomTypeService_AddRoomTypeAsync_AdminHasNoHotel_ThrowsUnAuthorizedException));
        var admin = new User { UserId = Guid.NewGuid(), Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow };
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();
        var sut = CreateRoomTypeSut(nameof(RoomTypeService_AddRoomTypeAsync_AdminHasNoHotel_ThrowsUnAuthorizedException), ctx);

        // Act
        var act = async () => await sut.AddRoomTypeAsync(admin.UserId, new CreateRoomTypeDto { Name = "Deluxe", MaxOccupancy = 2, AmenityIds = new List<Guid>() });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task RoomTypeService_GetRoomTypesByHotelAsync_ValidAdmin_ReturnsList()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RoomTypeService_GetRoomTypesByHotelAsync_ValidAdmin_ReturnsList));
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        ctx.Users.Add(admin);
        ctx.RoomTypes.Add(new RoomType { RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true });
        await ctx.SaveChangesAsync();
        var sut = CreateRoomTypeSut(nameof(RoomTypeService_GetRoomTypesByHotelAsync_ValidAdmin_ReturnsList), ctx);

        // Act
        var result = await sut.GetRoomTypesByHotelAsync(admin.UserId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task RoomTypeService_GetRoomTypesByHotelPagedAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RoomTypeService_GetRoomTypesByHotelPagedAsync_ValidAdmin_ReturnsPaged));
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        ctx.Users.Add(admin);
        ctx.RoomTypes.Add(new RoomType { RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true });
        await ctx.SaveChangesAsync();
        var sut = CreateRoomTypeSut(nameof(RoomTypeService_GetRoomTypesByHotelPagedAsync_ValidAdmin_ReturnsPaged), ctx);

        // Act
        var result = await sut.GetRoomTypesByHotelPagedAsync(admin.UserId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task RoomTypeService_ToggleRoomTypeStatusAsync_ValidRoomType_TogglesStatus()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RoomTypeService_ToggleRoomTypeStatusAsync_ValidRoomType_TogglesStatus));
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomTypeId = Guid.NewGuid();
        ctx.Users.Add(admin);
        ctx.RoomTypes.Add(new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true });
        await ctx.SaveChangesAsync();
        var sut = CreateRoomTypeSut(nameof(RoomTypeService_ToggleRoomTypeStatusAsync_ValidRoomType_TogglesStatus), ctx);

        // Act
        await sut.ToggleRoomTypeStatusAsync(admin.UserId, roomTypeId, false);

        // Assert
        var rt = await ctx.RoomTypes.FindAsync(roomTypeId);
        rt!.IsActive.Should().BeFalse();
    }

    // ── LogService — missing branches ─────────────────────────────────────────

    [Fact]
    public async Task LogService_GetAllLogsAsync_NoSearch_ReturnsAll()
    {
        // Arrange
        var logRepo = new Mock<IRepository<Guid, Log>>();
        var logs = new List<Log>
        {
            new() { LogId = Guid.NewGuid(), Message = "Error1", ExceptionType = "Ex", StackTrace = "st", StatusCode = 500, UserName = "User", Role = "Guest", Controller = "C", Action = "A", HttpMethod = "GET", RequestPath = "/test", CreatedAt = DateTime.UtcNow },
            new() { LogId = Guid.NewGuid(), Message = "Error2", ExceptionType = "Ex", StackTrace = "st", StatusCode = 400, UserName = "Admin", Role = "Admin", Controller = "C", Action = "A", HttpMethod = "POST", RequestPath = "/api", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        logRepo.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = new LogService(logRepo.Object);

        // Act
        var result = await sut.GetAllLogsAsync(1, 10, null);

        // Assert
        result.TotalCount.Should().Be(2);
    }

    // ── AuditLogService — LogAsync with null userId ───────────────────────────

    [Fact]
    public async Task AuditLogService_LogAsync_NullUserId_AddsEntry()
    {
        // Arrange
        var auditRepo = new Mock<IRepository<Guid, AuditLog>>();
        auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog al) => al);
        var uow = new Mock<IUnitOfWork>();
        var sut = new AuditLogService(auditRepo.Object, new Mock<IRepository<Guid, User>>().Object, uow.Object);

        // Act
        var act = async () => await sut.LogAsync(null, "Create", "Hotel", null, "{}");

        // Assert
        await act.Should().NotThrowAsync();
        auditRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
    }

    // ── DashboardService — GetGuestDashboardAsync with transactions ───────────

    [Fact]
    public async Task DashboardService_GetGuestDashboardAsync_WithSpend_ReturnsTotalSpent()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var reservations = new List<Reservation>
        {
            new() { ReservationId = reservationId, UserId = guestId, HotelId = hotelId, Status = ReservationStatus.Completed, TotalAmount = 1000, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        var transactions = new List<Transaction>
        {
            new() { TransactionId = Guid.NewGuid(), ReservationId = reservationId, Amount = 1000, PaymentMethod = PaymentMethod.UPI, Status = PaymentStatus.Success, TransactionDate = DateTime.UtcNow, Reservation = new Reservation { UserId = guestId, HotelId = hotelId, Status = ReservationStatus.Completed, TotalAmount = 1000, CheckInDate = DateOnly.FromDateTime(DateTime.Today), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), CreatedDate = DateTime.UtcNow } }
        }.AsQueryable().BuildMock();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        resRepo.Setup(r => r.GetQueryable()).Returns(reservations);
        var txRepo = new Mock<IRepository<Guid, Transaction>>();
        txRepo.Setup(r => r.GetQueryable()).Returns(transactions);
        var sut = new DashboardService(new Mock<IRepository<Guid, User>>().Object, new Mock<IRepository<Guid, Hotel>>().Object, resRepo.Object, txRepo.Object, new Mock<IRepository<Guid, Review>>().Object, new Mock<IRepository<Guid, Room>>().Object, new Mock<IRepository<Guid, RoomType>>().Object, new Mock<IRepository<Guid, SuperAdminRevenue>>().Object);

        // Act
        var result = await sut.GetGuestDashboardAsync(guestId);

        // Assert
        result.TotalBookings.Should().Be(1);
        result.CompletedBookings.Should().Be(1);
    }
}
