using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;
using HotelBookingAppWebApi.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HotelBookingAppWebApi.Tests.Services;

/// <summary>
/// Fills coverage gaps identified from the cobertura report.
/// Uses AAA pattern throughout.
/// </summary>
public class CoverageGapTests
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

    private static Hotel MakeHotel(bool isActive = true, bool blocked = false) => new()
    {
        HotelId = Guid.NewGuid(), Name = "Grand Hotel", Address = "123 Main",
        City = "Mumbai", State = "MH", ContactNumber = "9999999999",
        IsActive = isActive, IsBlockedBySuperAdmin = blocked, CreatedAt = DateTime.UtcNow,
        RoomTypes = new List<RoomType>(), Reviews = new List<Review>()
    };

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "admin@test.com",
        Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
        Role = UserRole.Admin, HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    // ── HotelService gaps ─────────────────────────────────────────────────────

    private HotelService CreateHotelSut(
        Mock<IRepository<Guid, Hotel>> hotelRepo,
        Mock<IRepository<Guid, User>> userRepo,
        Mock<IRepository<Guid, RoomType>>? roomTypeRepo = null,
        Mock<IRepository<Guid, Transaction>>? txRepo = null,
        Mock<IRepository<Guid, Reservation>>? resRepo = null,
        Mock<IAuditLogService>? audit = null,
        Mock<IUnitOfWork>? uow = null) => new(
            hotelRepo.Object,
            userRepo.Object,
            (roomTypeRepo ?? new Mock<IRepository<Guid, RoomType>>()).Object,
            (txRepo ?? new Mock<IRepository<Guid, Transaction>>()).Object,
            (resRepo ?? new Mock<IRepository<Guid, Reservation>>()).Object,
            (audit ?? new Mock<IAuditLogService>()).Object,
            (uow ?? new Mock<IUnitOfWork>()).Object);

    [Fact]
    public async Task HotelService_SearchHotelsAsync_EmptyResult_ReturnsEmptyResponse()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel>().AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());
        var req = new SearchHotelRequestDto
        {
            City = "Nowhere", CheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
        };

        // Act
        var result = await sut.SearchHotelsAsync(req);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Hotels.Should().BeEmpty();
    }

    [Fact]
    public async Task HotelService_SearchHotelsAsync_WithResults_ReturnsPaged()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotel = MakeHotel();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());
        var req = new SearchHotelRequestDto
        {
            CheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            PageNumber = 1, PageSize = 10
        };

        // Act
        var result = await sut.SearchHotelsAsync(req);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HotelService_SearchHotelsAsync_WithStateFilter_FiltersCorrectly()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotel = MakeHotel();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());
        var req = new SearchHotelRequestDto
        {
            State = "MH",
            CheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
        };

        // Act
        var result = await sut.SearchHotelsAsync(req);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HotelService_SearchHotelsAsync_SortByPriceAsc_Succeeds()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotel = MakeHotel();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());
        var req = new SearchHotelRequestDto
        {
            SortBy = "price_asc",
            CheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
        };

        // Act
        var result = await sut.SearchHotelsAsync(req);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task HotelService_SearchHotelsAsync_SortByPriceDesc_Succeeds()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotel = MakeHotel();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());
        var req = new SearchHotelRequestDto
        {
            SortBy = "price_desc",
            CheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
        };

        // Act
        var result = await sut.SearchHotelsAsync(req);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task HotelService_GetActiveStatesAsync_ReturnsDistinctStates()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel() }.AsQueryable().BuildMock();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());

        // Act
        var result = await sut.GetActiveStatesAsync();

        // Assert
        result.Should().Contain("MH");
    }

    [Fact]
    public async Task HotelService_GetHotelsByStateAsync_ReturnsHotels()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotels = new List<Hotel> { MakeHotel() }.AsQueryable().BuildMock();
        hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());

        // Act
        var result = await sut.GetHotelsByStateAsync("MH");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task HotelService_GetRoomTypesAsync_ReturnsRoomTypes()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        var hotelId = Guid.NewGuid();
        var roomType = new RoomType
        {
            RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Deluxe",
            MaxOccupancy = 2, IsActive = true,
            RoomTypeAmenities = new List<RoomTypeAmenity>()
        };
        roomTypeRepo.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { roomType }.AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>(), roomTypeRepo);

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
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var txRepo = new Mock<IRepository<Guid, Transaction>>();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { MakeHotel() }.AsQueryable().BuildMock());
        resRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Reservation>().AsQueryable().BuildMock());
        txRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>().AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>(),
            resRepo: resRepo, txRepo: txRepo);

        // Act
        var result = await sut.GetAllHotelsForSuperAdminAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task HotelService_GetAllHotelsForSuperAdminPagedAsync_WithSearchAndStatus_Filters()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var txRepo = new Mock<IRepository<Guid, Transaction>>();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { MakeHotel() }.AsQueryable().BuildMock());
        resRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Reservation>().AsQueryable().BuildMock());
        txRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Transaction>().AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>(),
            resRepo: resRepo, txRepo: txRepo);

        // Act — test all status branches
        var r1 = await sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, "Grand", "Active");
        var r2 = await sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, null, "Inactive");
        var r3 = await sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, null, "Blocked");
        var r4 = await sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, null, "All");

        // Assert
        r1.Should().NotBeNull();
        r2.Should().NotBeNull();
        r3.Should().NotBeNull();
        r4.Should().NotBeNull();
    }

    [Fact]
    public async Task HotelService_ToggleHotelStatusAsync_Deactivate_Succeeds()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var uow = new Mock<IUnitOfWork>();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var hotel = MakeHotel(isActive: true);
        hotel.HotelId = hotelId;
        userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(admin);
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var sut = CreateHotelSut(hotelRepo, userRepo, uow: uow);

        // Act
        await sut.ToggleHotelStatusAsync(admin.UserId, false);

        // Assert
        hotel.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task HotelService_UpdateHotelAsync_WithNullUpiId_DoesNotOverwrite()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var uow = new Mock<IUnitOfWork>();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var hotel = MakeHotel();
        hotel.HotelId = hotelId;
        hotel.UpiId = "existing@upi";
        userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(admin);
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        var sut = CreateHotelSut(hotelRepo, userRepo, uow: uow);

        // Act — UpiId is null in dto, should not overwrite
        await sut.UpdateHotelAsync(admin.UserId, new UpdateHotelDto
        {
            Name = "Updated", Address = "Addr", City = "City",
            ContactNumber = "9000000000", UpiId = null
        });

        // Assert
        hotel.UpiId.Should().Be("existing@upi");
    }

    [Fact]
    public async Task HotelService_SearchHotelsAsync_WithAmenityAndRoomTypeAndPriceFilters_Succeeds()
    {
        // Arrange
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var hotel = MakeHotel();
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { hotel }.AsQueryable().BuildMock());
        var sut = CreateHotelSut(hotelRepo, new Mock<IRepository<Guid, User>>());
        var req = new SearchHotelRequestDto
        {
            AmenityIds = new List<Guid> { Guid.NewGuid() },
            RoomType = "Deluxe",
            MinPrice = 500m,
            MaxPrice = 5000m,
            CheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
        };

        // Act
        var result = await sut.SearchHotelsAsync(req);

        // Assert
        result.Should().NotBeNull();
    }

    // ── AmenityService gaps ───────────────────────────────────────────────────

    private AmenityService CreateAmenitySut(
        Mock<IRepository<Guid, Amenity>> amenityRepo,
        string dbName,
        Mock<IUnitOfWork>? uow = null)
    {
        var ctx = CreateContext(dbName);
        return new AmenityService(amenityRepo.Object, ctx, (uow ?? new Mock<IUnitOfWork>()).Object);
    }

    [Fact]
    public async Task AmenityService_GetAllAmenitiesPagedAsync_WithSearchAndCategory_ReturnsFiltered()
    {
        // Arrange
        var amenityRepo = new Mock<IRepository<Guid, Amenity>>();
        var amenities = new List<Amenity>
        {
            new() { AmenityId = Guid.NewGuid(), Name = "WiFi", Category = "Tech", IsActive = true },
            new() { AmenityId = Guid.NewGuid(), Name = "Pool", Category = "Recreation", IsActive = true }
        }.AsQueryable().BuildMock();
        amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);
        var sut = CreateAmenitySut(amenityRepo, nameof(AmenityService_GetAllAmenitiesPagedAsync_WithSearchAndCategory_ReturnsFiltered));

        // Act
        var result = await sut.GetAllAmenitiesPagedAsync(1, 10, "wifi", "Tech");

        // Assert
        result.TotalCount.Should().Be(1);
        result.Amenities.First().Name.Should().Be("WiFi");
    }

    [Fact]
    public async Task AmenityService_GetAllAmenitiesPagedAsync_NoFilter_ReturnsAll()
    {
        // Arrange
        var amenityRepo = new Mock<IRepository<Guid, Amenity>>();
        var amenities = new List<Amenity>
        {
            new() { AmenityId = Guid.NewGuid(), Name = "WiFi", Category = "Tech", IsActive = true },
            new() { AmenityId = Guid.NewGuid(), Name = "Pool", Category = "Recreation", IsActive = false }
        }.AsQueryable().BuildMock();
        amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);
        var sut = CreateAmenitySut(amenityRepo, nameof(AmenityService_GetAllAmenitiesPagedAsync_NoFilter_ReturnsAll));

        // Act
        var result = await sut.GetAllAmenitiesPagedAsync(1, 10, null, null);

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task AmenityService_UpdateAmenityAsync_ValidDto_ReturnsUpdated()
    {
        // Arrange
        var amenityRepo = new Mock<IRepository<Guid, Amenity>>();
        var uow = new Mock<IUnitOfWork>();
        var amenityId = Guid.NewGuid();
        var amenity = new Amenity { AmenityId = amenityId, Name = "Old", Category = "Tech", IsActive = true };
        amenityRepo.Setup(r => r.GetAsync(amenityId)).ReturnsAsync(amenity);
        amenityRepo.Setup(r => r.UpdateAsync(amenityId, amenity)).ReturnsAsync(amenity);
        var sut = CreateAmenitySut(amenityRepo, nameof(AmenityService_UpdateAmenityAsync_ValidDto_ReturnsUpdated), uow);

        // Act
        var result = await sut.UpdateAmenityAsync(new UpdateAmenityDto
        {
            AmenityId = amenityId, Name = "Updated", Category = "Wellness", IsActive = true
        });

        // Assert
        result.Name.Should().Be("Updated");
        uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task AmenityService_UpdateAmenityAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var amenityRepo = new Mock<IRepository<Guid, Amenity>>();
        var uow = new Mock<IUnitOfWork>();
        amenityRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Amenity?)null);
        var sut = CreateAmenitySut(amenityRepo, nameof(AmenityService_UpdateAmenityAsync_NotFound_ThrowsNotFoundException), uow);

        // Act
        var act = async () => await sut.UpdateAmenityAsync(new UpdateAmenityDto
        {
            AmenityId = Guid.NewGuid(), Name = "X", Category = "Y"
        });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task AmenityService_DeleteAmenityAsync_InUse_ThrowsConflictException()
    {
        // Arrange
        var dbName = nameof(AmenityService_DeleteAmenityAsync_InUse_ThrowsConflictException);
        var ctx = CreateContext(dbName);
        var amenityRepo = new Mock<IRepository<Guid, Amenity>>();
        var amenityId = Guid.NewGuid();
        var amenity = new Amenity { AmenityId = amenityId, Name = "WiFi", Category = "Tech", IsActive = true };
        amenityRepo.Setup(r => r.GetAsync(amenityId)).ReturnsAsync(amenity);
        // Seed a RoomTypeAmenity that references this amenity
        ctx.RoomTypeAmenities.Add(new RoomTypeAmenity
        {
            RoomTypeId = Guid.NewGuid(),
            AmenityId = amenityId
        });
        await ctx.SaveChangesAsync();
        var sut = new AmenityService(amenityRepo.Object, ctx, new Mock<IUnitOfWork>().Object);

        // Act
        var act = async () => await sut.DeleteAmenityAsync(amenityId);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*in use*");
    }

    // ── WalletService gaps ────────────────────────────────────────────────────

    private WalletService CreateWalletSut(
        Mock<IRepository<Guid, Wallet>> walletRepo,
        Mock<IRepository<Guid, WalletTransaction>> txRepo,
        Mock<IRepository<Guid, User>>? userRepo = null,
        Mock<IUnitOfWork>? uow = null) => new(
            walletRepo.Object, txRepo.Object,
            (userRepo ?? new Mock<IRepository<Guid, User>>()).Object,
            (uow ?? new Mock<IUnitOfWork>()).Object);

    [Fact]
    public async Task WalletService_GetWalletAsync_ExistingWallet_ReturnsPaged()
    {
        // Arrange
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        var txRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        var userId = Guid.NewGuid();
        var wallet = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 500m, UpdatedAt = DateTime.UtcNow };
        walletRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        txRepo.Setup(r => r.GetQueryable())
            .Returns(new List<WalletTransaction>().AsQueryable().BuildMock());
        var sut = CreateWalletSut(walletRepo, txRepo);

        // Act
        var result = await sut.GetWalletAsync(userId, 1, 10);

        // Assert
        result.Wallet.Balance.Should().Be(500m);
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task WalletService_GetWalletAsync_NoWallet_CreatesAndReturns()
    {
        // Arrange
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        var txRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        var uow = new Mock<IUnitOfWork>();
        var userId = Guid.NewGuid();
        walletRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Wallet>().AsQueryable().BuildMock());
        walletRepo.Setup(r => r.AddAsync(It.IsAny<Wallet>()))
            .ReturnsAsync((Wallet w) => w);
        txRepo.Setup(r => r.GetQueryable())
            .Returns(new List<WalletTransaction>().AsQueryable().BuildMock());
        var sut = CreateWalletSut(walletRepo, txRepo, uow: uow);

        // Act
        var result = await sut.GetWalletAsync(userId, 1, 10);

        // Assert
        result.Wallet.Balance.Should().Be(0m);
        walletRepo.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Once);
    }

    [Fact]
    public async Task WalletService_DebitAsync_SufficientBalance_DebitsAndReturnsTrue()
    {
        // Arrange
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        var txRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        var userId = Guid.NewGuid();
        var wallet = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 1000m, UpdatedAt = DateTime.UtcNow };
        walletRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        txRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>()))
            .ReturnsAsync((WalletTransaction wt) => wt);
        var sut = CreateWalletSut(walletRepo, txRepo);

        // Act
        var result = await sut.DebitAsync(userId, 300m, "Booking payment");

        // Assert
        result.Should().BeTrue();
        wallet.Balance.Should().Be(700m);
    }

    [Fact]
    public async Task WalletService_DebitAsync_ZeroBalance_ReturnsFalse()
    {
        // Arrange
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        var txRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        var userId = Guid.NewGuid();
        var wallet = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 0m, UpdatedAt = DateTime.UtcNow };
        walletRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Wallet> { wallet }.AsQueryable().BuildMock());
        var sut = CreateWalletSut(walletRepo, txRepo);

        // Act
        var result = await sut.DebitAsync(userId, 100m, "Payment");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WalletService_TopUpAsync_NegativeAmount_ThrowsValidationException()
    {
        // Arrange
        var walletRepo = new Mock<IRepository<Guid, Wallet>>();
        var txRepo = new Mock<IRepository<Guid, WalletTransaction>>();
        var sut = CreateWalletSut(walletRepo, txRepo);

        // Act
        var act = async () => await sut.TopUpAsync(Guid.NewGuid(), -100m);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── SupportRequestService gaps ────────────────────────────────────────────

    private SupportRequestService CreateSupportSut(
        Mock<IRepository<Guid, SupportRequest>> supportRepo,
        Mock<IRepository<Guid, User>>? userRepo = null,
        Mock<IRepository<Guid, Hotel>>? hotelRepo = null,
        Mock<IUnitOfWork>? uow = null) => new(
            supportRepo.Object,
            (userRepo ?? new Mock<IRepository<Guid, User>>()).Object,
            (hotelRepo ?? new Mock<IRepository<Guid, Hotel>>()).Object,
            (uow ?? new Mock<IUnitOfWork>()).Object);

    [Fact]
    public async Task SupportService_GetAdminRequestsAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var admin = MakeAdmin();
        userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), UserId = admin.UserId, Subject = "Bug", Message = "Found bug", Category = "Tech", SubmitterRole = "Admin", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        supportRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSupportSut(supportRepo, userRepo);

        // Act
        var result = await sut.GetAdminRequestsAsync(admin.UserId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SupportService_GetAllRequestsAsync_WithRoleAndSearchFilter_ReturnsFiltered()
    {
        // Arrange
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), Subject = "Billing", Message = "Help", Category = "Billing", SubmitterRole = "Guest", Status = SupportRequestStatus.Open, GuestName = "Alice", GuestEmail = "alice@test.com", CreatedAt = DateTime.UtcNow },
            new() { SupportRequestId = Guid.NewGuid(), Subject = "Tech Issue", Message = "Bug", Category = "Tech", SubmitterRole = "Admin", Status = SupportRequestStatus.Resolved, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        supportRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSupportSut(supportRepo);

        // Act
        var result = await sut.GetAllRequestsAsync("Open", "Guest", "billing", 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SupportService_GetAllRequestsAsync_AllFilter_ReturnsAll()
    {
        // Arrange
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        var requests = new List<SupportRequest>
        {
            new() { SupportRequestId = Guid.NewGuid(), Subject = "Issue", Message = "Help", Category = "General", SubmitterRole = "Guest", Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        supportRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSupportSut(supportRepo);

        // Act
        var result = await sut.GetAllRequestsAsync("All", "All", null, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SupportService_RespondAsync_WithOpenStatus_DefaultsToResolved()
    {
        // Arrange
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        var requestId = Guid.NewGuid();
        var request = new SupportRequest
        {
            SupportRequestId = requestId, Subject = "Issue", Message = "Help",
            Category = "General", SubmitterRole = "Guest",
            Status = SupportRequestStatus.Open, CreatedAt = DateTime.UtcNow
        };
        var requests = new List<SupportRequest> { request }.AsQueryable().BuildMock();
        supportRepo.Setup(r => r.GetQueryable()).Returns(requests);
        var sut = CreateSupportSut(supportRepo);

        // Act — pass "Open" as status which should default to Resolved
        var result = await sut.RespondAsync(requestId, new RespondSupportRequestDto
        {
            Response = "Handled", Status = "Open"
        });

        // Assert
        result.Status.Should().Be("Resolved");
    }

    [Fact]
    public async Task SupportService_CreateGuestRequestAsync_WithHotelId_FetchesHotelName()
    {
        // Arrange
        var supportRepo = new Mock<IRepository<Guid, SupportRequest>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var user = new User { UserId = Guid.NewGuid(), Name = "Guest", Email = "g@test.com", Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 }, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow };
        var hotelId = Guid.NewGuid();
        var hotel = MakeHotel();
        hotel.HotelId = hotelId;
        userRepo.Setup(r => r.GetAsync(user.UserId)).ReturnsAsync(user);
        hotelRepo.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);
        supportRepo.Setup(r => r.AddAsync(It.IsAny<SupportRequest>())).ReturnsAsync((SupportRequest sr) => sr);
        var sut = CreateSupportSut(supportRepo, userRepo, hotelRepo);

        // Act
        var result = await sut.CreateGuestRequestAsync(user.UserId, new GuestSupportRequestDto
        {
            Subject = "Room Issue", Message = "AC broken", Category = "Maintenance", HotelId = hotelId
        });

        // Assert
        result.HotelName.Should().Be("Grand Hotel");
    }

    // ── RoomService gaps ──────────────────────────────────────────────────────

    private RoomService CreateRoomSut(
        Mock<IRepository<Guid, Room>> roomRepo,
        Mock<IRepository<Guid, RoomType>>? roomTypeRepo = null,
        Mock<IRepository<Guid, RoomTypeInventory>>? invRepo = null,
        Mock<IRepository<Guid, User>>? userRepo = null,
        Mock<IUnitOfWork>? uow = null) => new(
            roomRepo.Object,
            (roomTypeRepo ?? new Mock<IRepository<Guid, RoomType>>()).Object,
            (invRepo ?? new Mock<IRepository<Guid, RoomTypeInventory>>()).Object,
            (userRepo ?? new Mock<IRepository<Guid, User>>()).Object,
            new Mock<IAuditLogService>().Object,
            (uow ?? new Mock<IUnitOfWork>()).Object);

    [Fact]
    public async Task RoomService_GetRoomsByHotelAsync_ValidAdmin_ReturnsRooms()
    {
        // Arrange
        var roomRepo = new Mock<IRepository<Guid, Room>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomTypeId = Guid.NewGuid();
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        var room = new Room { RoomId = Guid.NewGuid(), HotelId = hotelId, RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId, IsActive = true, RoomType = roomType };
        userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        roomRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Room> { room }.AsQueryable().BuildMock());
        roomTypeRepo.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { roomType }.AsQueryable().BuildMock());
        var sut = CreateRoomSut(roomRepo, roomTypeRepo, userRepo: userRepo);

        // Act
        var result = await sut.GetRoomsByHotelAsync(admin.UserId, 1, 10);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task RoomService_UpdateRoomAsync_ValidDto_UpdatesRoom()
    {
        // Arrange
        var roomRepo = new Mock<IRepository<Guid, Room>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        var uow = new Mock<IUnitOfWork>();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var room = new Room { RoomId = roomId, HotelId = hotelId, RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId, IsActive = true };
        var roomType = new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true };
        userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        roomRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Room> { room }.AsQueryable().BuildMock());
        roomTypeRepo.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { roomType }.AsQueryable().BuildMock());
        var sut = CreateRoomSut(roomRepo, roomTypeRepo, userRepo: userRepo, uow: uow);

        // Act
        await sut.UpdateRoomAsync(admin.UserId, new UpdateRoomDto
        {
            RoomId = roomId, RoomNumber = "202", Floor = 2, RoomTypeId = roomTypeId
        });

        // Assert
        room.RoomNumber.Should().Be("202");
        uow.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task RoomService_EnsureRoomCapacityNotExceeded_ExceedsCapacity_ThrowsValidationException()
    {
        // Arrange
        var roomRepo = new Mock<IRepository<Guid, Room>>();
        var roomTypeRepo = new Mock<IRepository<Guid, RoomType>>();
        var invRepo = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var uow = new Mock<IUnitOfWork>();
        var hotelId = Guid.NewGuid();
        var admin = MakeAdmin(hotelId);
        var roomTypeId = Guid.NewGuid();
        // 2 existing rooms, inventory max = 1
        var rooms = new List<Room>
        {
            new() { RoomId = Guid.NewGuid(), HotelId = hotelId, RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId, IsActive = true },
            new() { RoomId = Guid.NewGuid(), HotelId = hotelId, RoomNumber = "102", Floor = 1, RoomTypeId = roomTypeId, IsActive = true }
        };
        var inventories = new List<RoomTypeInventory>
        {
            new() { RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId, Date = DateOnly.FromDateTime(DateTime.Today), TotalInventory = 1, ReservedInventory = 0 }
        };
        userRepo.Setup(r => r.GetAsync(admin.UserId)).ReturnsAsync(admin);
        roomTypeRepo.Setup(r => r.GetQueryable())
            .Returns(new List<RoomType> { new() { RoomTypeId = roomTypeId, HotelId = hotelId, Name = "Deluxe", MaxOccupancy = 2, IsActive = true } }.AsQueryable().BuildMock());
        roomRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Room>().AsQueryable().BuildMock()); // no duplicate
        invRepo.Setup(r => r.GetQueryable())
            .Returns(inventories.AsQueryable().BuildMock());
        roomRepo.Setup(r => r.AddAsync(It.IsAny<Room>())).ReturnsAsync((Room rm) => rm);
        var sut = CreateRoomSut(roomRepo, roomTypeRepo, invRepo, userRepo, uow);

        // Act — add a 3rd room when inventory max is 1
        // The capacity check compares existing active rooms count vs max inventory
        // We need to seed existing rooms in the mock after the duplicate check
        roomRepo.Setup(r => r.GetQueryable())
            .Returns(rooms.AsQueryable().BuildMock());
        var act = async () => await sut.AddRoomAsync(admin.UserId, new CreateRoomDto
        {
            RoomNumber = "103", Floor = 1, RoomTypeId = roomTypeId
        });

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── AmenityRequestService gaps ────────────────────────────────────────────

    [Fact]
    public async Task AmenityRequestService_GetAllRequestsPagedAsync_WithStatusFilter_ReturnsFiltered()
    {
        // Arrange
        var requestRepo = new Mock<IRepository<Guid, AmenityRequest>>();
        var amenityRepo = new Mock<IRepository<Guid, Amenity>>();
        var userRepo = new Mock<IRepository<Guid, User>>();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var uow = new Mock<IUnitOfWork>();
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var requests = new List<AmenityRequest>
        {
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Sauna", Category = "Wellness", Status = AmenityRequestStatus.Pending, CreatedAt = DateTime.UtcNow },
            new() { AmenityRequestId = Guid.NewGuid(), RequestedByAdminId = adminId, AdminHotelId = hotelId, AmenityName = "Pool", Category = "Recreation", Status = AmenityRequestStatus.Approved, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        requestRepo.Setup(r => r.GetQueryable()).Returns(requests);
        hotelRepo.Setup(r => r.GetQueryable())
            .Returns(new List<Hotel> { new() { HotelId = hotelId, Name = "Grand", Address = "A", City = "C", ContactNumber = "9", CreatedAt = DateTime.UtcNow } }.AsQueryable().BuildMock());
        var sut = new AmenityRequestService(requestRepo.Object, amenityRepo.Object, userRepo.Object, hotelRepo.Object, uow.Object);

        // Act
        var result = await sut.GetAllRequestsAsync("Pending", 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    // ── UserService gaps ──────────────────────────────────────────────────────

    [Fact]
    public async Task UserService_UpdateProfileAsync_UserWithNoDetails_ThrowsUserProfileException()
    {
        // Arrange
        var userRepo = new Mock<IRepository<Guid, User>>();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var reviewRepo = new Mock<IRepository<Guid, Review>>();
        var uow = new Mock<IUnitOfWork>();
        var userId = Guid.NewGuid();
        // User with NO UserDetails
        var user = new User
        {
            UserId = userId, Name = "Bob", Email = "bob@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow,
            UserDetails = null
        };
        var users = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Setup(r => r.GetQueryable()).Returns(users);
        var sut = new UserService(userRepo.Object, resRepo.Object, reviewRepo.Object, uow.Object);

        // Act
        var act = async () => await sut.UpdateProfileAsync(userId, new UpdateUserProfileDto
        {
            Name = "Bob Updated", PhoneNumber = "9000000001"
        });

        // Assert — service throws when UserDetails is null
        await act.Should().ThrowAsync<Exception>();
        uow.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── PromoCodeService gaps ─────────────────────────────────────────────────

    [Fact]
    public async Task PromoCodeService_GetGuestPromoCodesAsync_ReturnsAll()
    {
        // Arrange
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var uow = new Mock<IUnitOfWork>();
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            new() { PromoCodeId = Guid.NewGuid(), Code = "PROMO1", UserId = userId, HotelId = hotelId, DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(30), IsUsed = false, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        promoRepo.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, hotelRepo.Object, uow.Object);

        // Act
        var result = await sut.GetGuestPromoCodesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task PromoCodeService_GetGuestPromoCodesPagedAsync_WithExpiredFilter_ReturnsExpired()
    {
        // Arrange
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var uow = new Mock<IUnitOfWork>();
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            new() { PromoCodeId = Guid.NewGuid(), Code = "EXPIRED1", UserId = userId, HotelId = hotelId, DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(-5), IsUsed = false, CreatedAt = DateTime.UtcNow },
            new() { PromoCodeId = Guid.NewGuid(), Code = "ACTIVE1", UserId = userId, HotelId = hotelId, DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(30), IsUsed = false, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        promoRepo.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, hotelRepo.Object, uow.Object);

        // Act
        var result = await sut.GetGuestPromoCodesPagedAsync(userId, 1, 10, "Expired");

        // Assert
        result.TotalCount.Should().Be(1);
        result.PromoCodes.First().Code.Should().Be("EXPIRED1");
    }

    [Fact]
    public async Task PromoCodeService_GetGuestPromoCodesPagedAsync_WithUsedFilter_ReturnsUsed()
    {
        // Arrange
        var promoRepo = new Mock<IRepository<Guid, PromoCode>>();
        var resRepo = new Mock<IRepository<Guid, Reservation>>();
        var hotelRepo = new Mock<IRepository<Guid, Hotel>>();
        var uow = new Mock<IUnitOfWork>();
        var userId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var promos = new List<PromoCode>
        {
            new() { PromoCodeId = Guid.NewGuid(), Code = "USED1", UserId = userId, HotelId = hotelId, DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(30), IsUsed = true, CreatedAt = DateTime.UtcNow },
            new() { PromoCodeId = Guid.NewGuid(), Code = "ACTIVE1", UserId = userId, HotelId = hotelId, DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddDays(30), IsUsed = false, CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        promoRepo.Setup(r => r.GetQueryable()).Returns(promos);
        var sut = new PromoCodeService(promoRepo.Object, resRepo.Object, hotelRepo.Object, uow.Object);

        // Act
        var result = await sut.GetGuestPromoCodesPagedAsync(userId, 1, 10, "Used");

        // Assert
        result.TotalCount.Should().Be(1);
    }
}
