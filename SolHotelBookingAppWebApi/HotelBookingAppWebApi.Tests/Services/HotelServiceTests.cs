using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class HotelServiceTests
{
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepo = new();
    private readonly Mock<IRepository<Guid, Transaction>> _transactionRepo = new();
    private readonly Mock<IRepository<Guid, Reservation>> _reservationRepo = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly HotelService _sut;

    public HotelServiceTests()
    {
        _sut = new HotelService(
            _hotelRepo.Object, _userRepo.Object, _roomTypeRepo.Object,
            _transactionRepo.Object, _reservationRepo.Object,
            _auditLog.Object, _unitOfWork.Object);
    }

    private static Hotel MakeHotel(Guid? id = null, bool isActive = true, bool isBlocked = false) => new()
    {
        HotelId = id ?? Guid.NewGuid(), Name = "Grand Hotel", Address = "123 Main St",
        City = "Chennai", State = "TN", Description = "Luxury", ContactNumber = "9999999999",
        IsActive = isActive, IsBlockedBySuperAdmin = isBlocked, CreatedAt = DateTime.UtcNow,
        GstPercent = 18m, Reviews = new List<Review>(), RoomTypes = new List<RoomType>()
    };

    private static User MakeAdmin(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(), Name = "Admin", Email = "a@a.com",
        HotelId = hotelId ?? Guid.NewGuid(), CreatedAt = DateTime.UtcNow
    };

    // ── GetTopHotelsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetTopHotelsAsync_ActiveHotels_ReturnsTop10()
    {
        // Arrange
        var hotels = Enumerable.Range(1, 5).Select(i =>
        {
            var h = MakeHotel();
            h.Name = $"Hotel {i}";
            h.RoomTypes = new List<RoomType>();
            return h;
        }).ToList().AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act
        var result = await _sut.GetTopHotelsAsync();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetTopHotelsAsync_BlockedHotels_ExcludesBlocked()
    {
        // Arrange
        var hotels = new List<Hotel>
        {
            MakeHotel(isActive: true, isBlocked: false),
            MakeHotel(isActive: true, isBlocked: true)
        }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act
        var result = await _sut.GetTopHotelsAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    // ── GetCitiesAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCitiesAsync_ActiveHotels_ReturnsDistinctCities()
    {
        // Arrange
        var hotels = new List<Hotel>
        {
            MakeHotel(), MakeHotel(), MakeHotel()
        };
        hotels[0].City = "Chennai"; hotels[1].City = "Mumbai"; hotels[2].City = "Chennai";
        var mock = hotels.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(mock);

        // Act
        var result = await _sut.GetCitiesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Chennai").And.Contain("Mumbai");
    }

    // ── GetHotelsByCityAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetHotelsByCityAsync_MatchingCity_ReturnsHotels()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel() }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act
        var result = await _sut.GetHotelsByCityAsync("Chennai");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHotelsByCityAsync_CaseInsensitive_ReturnsHotels()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel() }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act
        var result = await _sut.GetHotelsByCityAsync("CHENNAI");

        // Assert
        result.Should().HaveCount(1);
    }

    // ── GetHotelDetailsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetHotelDetailsAsync_ExistingHotel_ReturnsDetails()
    {
        // Arrange
        var hotel = MakeHotel();
        hotel.RoomTypes = new List<RoomType>();
        hotel.Reviews = new List<Review>();
        var hotels = new List<Hotel> { hotel }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act
        var result = await _sut.GetHotelDetailsAsync(hotel.HotelId);

        // Assert
        result.HotelId.Should().Be(hotel.HotelId);
        result.Name.Should().Be("Grand Hotel");
    }

    [Fact]
    public async Task GetHotelDetailsAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var hotels = new List<Hotel>().AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act & Assert
        await _sut.Invoking(s => s.GetHotelDetailsAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>().WithMessage("*Hotel*");
    }

    // ── GetRoomTypesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoomTypesAsync_ActiveRoomTypes_ReturnsRoomTypes()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypes = new List<RoomType>
        {
            new() { RoomTypeId = Guid.NewGuid(), HotelId = hotelId, Name = "Deluxe", IsActive = true, RoomTypeAmenities = new List<RoomTypeAmenity>() }
        }.AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act
        var result = await _sut.GetRoomTypesAsync(hotelId);

        // Assert
        result.Should().HaveCount(1);
    }

    // ── UpdateHotelAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateHotelAsync_ValidAdmin_UpdatesHotel()
    {
        // Arrange
        var admin = MakeAdmin();
        var hotel = MakeHotel(admin.HotelId);
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId!.Value)).ReturnsAsync(hotel);

        var dto = new UpdateHotelDto { Name = "Updated Hotel", Address = "456 New St", City = "Mumbai", Description = "Updated", ContactNumber = "8888888888" };

        // Act
        await _sut.UpdateHotelAsync(admin.UserId, dto);

        // Assert
        hotel.Name.Should().Be("Updated Hotel");
        hotel.City.Should().Be("Mumbai");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateHotelAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateHotelAsync(Guid.NewGuid(), new UpdateHotelDto { Name = "X", Address = "A", City = "C", Description = "D", ContactNumber = "1" }))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateHotelAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateHotelAsync(admin.UserId, new UpdateHotelDto { Name = "X", Address = "A", City = "C", Description = "D", ContactNumber = "1" }))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── ToggleHotelStatusAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ToggleHotelStatusAsync_Deactivate_SetsInactive()
    {
        // Arrange
        var admin = MakeAdmin();
        var hotel = MakeHotel(admin.HotelId, isActive: true);
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId!.Value)).ReturnsAsync(hotel);

        // Act
        await _sut.ToggleHotelStatusAsync(admin.UserId, false);

        // Assert
        hotel.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleHotelStatusAsync_ActivateBlockedHotel_ThrowsValidation()
    {
        // Arrange
        var admin = MakeAdmin();
        var hotel = MakeHotel(admin.HotelId, isActive: false, isBlocked: true);
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId!.Value)).ReturnsAsync(hotel);

        // Act & Assert
        await _sut.Invoking(s => s.ToggleHotelStatusAsync(admin.UserId, true))
            .Should().ThrowAsync<ValidationException>().WithMessage("*blocked*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleHotelStatusAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.ToggleHotelStatusAsync(Guid.NewGuid(), true))
            .Should().ThrowAsync<UnAuthorizedException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── BlockHotelAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task BlockHotelAsync_ExistingHotel_BlocksAndDeactivates()
    {
        // Arrange
        var hotel = MakeHotel(isActive: true);
        _hotelRepo.Setup(r => r.GetAsync(hotel.HotelId)).ReturnsAsync(hotel);

        // Act
        await _sut.BlockHotelAsync(hotel.HotelId);

        // Assert
        hotel.IsBlockedBySuperAdmin.Should().BeTrue();
        hotel.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task BlockHotelAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _hotelRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Hotel?)null);

        // Act & Assert
        await _sut.Invoking(s => s.BlockHotelAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── UnblockHotelAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UnblockHotelAsync_BlockedHotel_Unblocks()
    {
        // Arrange
        var hotel = MakeHotel(isBlocked: true);
        _hotelRepo.Setup(r => r.GetAsync(hotel.HotelId)).ReturnsAsync(hotel);

        // Act
        await _sut.UnblockHotelAsync(hotel.HotelId);

        // Assert
        hotel.IsBlockedBySuperAdmin.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UnblockHotelAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _hotelRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Hotel?)null);

        // Act & Assert
        await _sut.Invoking(s => s.UnblockHotelAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── UpdateHotelGstAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateHotelGstAsync_ValidAdmin_UpdatesGst()
    {
        // Arrange
        var admin = MakeAdmin();
        var hotel = MakeHotel(admin.HotelId);
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);
        _hotelRepo.Setup(r => r.GetAsync(admin.HotelId!.Value)).ReturnsAsync(hotel);

        // Act
        await _sut.UpdateHotelGstAsync(admin.UserId, 12m);

        // Assert
        hotel.GstPercent.Should().Be(12m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateHotelGstAsync_AdminNotFound_ThrowsUnauthorized()
    {
        // Arrange
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateHotelGstAsync(Guid.NewGuid(), 18m))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task UpdateHotelGstAsync_AdminNoHotel_ThrowsUnauthorized()
    {
        // Arrange
        var admin = new User { UserId = Guid.NewGuid(), HotelId = null, Name = "A", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(admin);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateHotelGstAsync(admin.UserId, 18m))
            .Should().ThrowAsync<UnAuthorizedException>().WithMessage("*No hotel*");
    }

    // ── GetAllHotelsForSuperAdminAsync ────────────────────────────────────────

    [Fact]
    public async Task GetAllHotelsForSuperAdminAsync_ReturnsAllHotels()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel() }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetAllHotelsForSuperAdminAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    // ── GetAllHotelsForSuperAdminPagedAsync ───────────────────────────────────

    [Fact]
    public async Task GetAllHotelsForSuperAdminPagedAsync_NoFilter_ReturnsPaged()
    {
        // Arrange
        var hotels = new List<Hotel> { MakeHotel(), MakeHotel() }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetAllHotelsForSuperAdminPagedAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Hotels.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllHotelsForSuperAdminPagedAsync_SearchFilter_FiltersCorrectly()
    {
        // Arrange
        var h1 = MakeHotel(); h1.Name = "Grand Palace";
        var h2 = MakeHotel(); h2.Name = "Budget Inn";
        var hotels = new List<Hotel> { h1, h2 }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, search: "Grand");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllHotelsForSuperAdminPagedAsync_ActiveStatusFilter_FiltersCorrectly()
    {
        // Arrange
        var h1 = MakeHotel(isActive: true, isBlocked: false);
        var h2 = MakeHotel(isActive: false, isBlocked: false);
        var hotels = new List<Hotel> { h1, h2 }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, status: "Active");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllHotelsForSuperAdminPagedAsync_BlockedStatusFilter_FiltersCorrectly()
    {
        // Arrange
        var h1 = MakeHotel(isActive: false, isBlocked: true);
        var h2 = MakeHotel(isActive: true, isBlocked: false);
        var hotels = new List<Hotel> { h1, h2 }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var reservations = new List<Reservation>().AsQueryable().BuildMock();
        _reservationRepo.Setup(r => r.GetQueryable()).Returns(reservations);

        var transactions = new List<Transaction>().AsQueryable().BuildMock();
        _transactionRepo.Setup(r => r.GetQueryable()).Returns(transactions);

        // Act
        var result = await _sut.GetAllHotelsForSuperAdminPagedAsync(1, 10, status: "Blocked");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    // ── GetActiveStatesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveStatesAsync_ReturnsDistinctStates()
    {
        // Arrange
        var h1 = MakeHotel(); h1.State = "TN";
        var h2 = MakeHotel(); h2.State = "MH";
        var h3 = MakeHotel(); h3.State = "TN";
        var hotels = new List<Hotel> { h1, h2, h3 }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act
        var result = await _sut.GetActiveStatesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("TN").And.Contain("MH");
    }

    // ── GetHotelsByStateAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetHotelsByStateAsync_MatchingState_ReturnsHotels()
    {
        // Arrange
        var h1 = MakeHotel(); h1.State = "TN";
        var h2 = MakeHotel(); h2.State = "MH";
        var hotels = new List<Hotel> { h1, h2 }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        // Act
        var result = await _sut.GetHotelsByStateAsync("TN");

        // Assert
        result.Should().HaveCount(1);
    }

    // ── SearchHotelsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SearchHotelsAsync_MatchingCity_ReturnsResults()
    {
        // Arrange
        var hotel = MakeHotel();
        hotel.RoomTypes = new List<RoomType>();
        hotel.Reviews = new List<Review>();
        var hotels = new List<Hotel> { hotel }.AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var request = new HotelBookingAppWebApi.Models.DTOs.Hotel.Public.SearchHotelRequestDto
        {
            City = "Chennai", PageNumber = 1, PageSize = 10
        };

        // Act
        var result = await _sut.SearchHotelsAsync(request);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Hotels.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchHotelsAsync_NoMatch_ThrowsNotFoundException()
    {
        // Arrange
        var hotels = new List<Hotel>().AsQueryable().BuildMock();
        _hotelRepo.Setup(r => r.GetQueryable()).Returns(hotels);

        var request = new HotelBookingAppWebApi.Models.DTOs.Hotel.Public.SearchHotelRequestDto
        {
            City = "NonExistentCity", PageNumber = 1, PageSize = 10
        };

        // Act & Assert
        await _sut.Invoking(s => s.SearchHotelsAsync(request))
            .Should().ThrowAsync<NotFoundException>().WithMessage("*No hotels*");
    }

    // ── GetAvailabilityAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailabilityAsync_NoInventory_ReturnsEmpty()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypes = new List<RoomType>().AsQueryable().BuildMock();
        _roomTypeRepo.Setup(r => r.GetQueryable()).Returns(roomTypes);

        // Act
        var result = await _sut.GetAvailabilityAsync(hotelId, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 11));

        // Assert
        result.Should().BeEmpty();
    }
}
