using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class AmenityServiceTests : IDisposable
{
    private readonly Mock<IRepository<Guid, Amenity>> _amenityRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly HotelBookingContext _context;
    private readonly AmenityService _sut;

    public AmenityServiceTests()
    {
        var options = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new HotelBookingContext(options);
        _sut = new AmenityService(_amenityRepo.Object, _context, _unitOfWork.Object);
    }

    public void Dispose() => _context.Dispose();

    private static Amenity MakeAmenity(string name = "WiFi", bool isActive = true) => new()
    {
        AmenityId = Guid.NewGuid(),
        Name = name,
        Category = "Tech",
        IconName = "wifi",
        IsActive = isActive
    };

    // ── GetAllActiveAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveAmenities()
    {
        // Arrange
        var amenities = new List<Amenity>
        {
            MakeAmenity("WiFi", isActive: true),
            MakeAmenity("Pool", isActive: false)
        }.AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);

        // Act
        var result = await _sut.GetAllActiveAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("WiFi");
    }

    [Fact]
    public async Task GetAllActiveAsync_NoActiveAmenities_ReturnsEmpty()
    {
        // Arrange
        var amenities = new List<Amenity>
        {
            MakeAmenity("Pool", isActive: false)
        }.AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);

        // Act
        var result = await _sut.GetAllActiveAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── SearchAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_MatchingQuery_ReturnsResults()
    {
        // Arrange
        var amenities = new List<Amenity>
        {
            MakeAmenity("WiFi"),
            MakeAmenity("Pool")
        }.AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);

        // Act
        var result = await _sut.SearchAsync("wifi");

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("WiFi");
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        // Arrange & Act
        var result = await _sut.SearchAsync("");

        // Assert
        result.Should().BeEmpty();
        _amenityRepo.Verify(r => r.GetQueryable(), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ReturnsEmpty()
    {
        // Arrange & Act
        var result = await _sut.SearchAsync("   ");

        // Assert
        result.Should().BeEmpty();
    }

    // ── CreateAmenityAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAmenityAsync_NewName_CreatesAmenity()
    {
        // Arrange
        var amenities = new List<Amenity>().AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);
        _amenityRepo.Setup(r => r.AddAsync(It.IsAny<Amenity>())).ReturnsAsync((Amenity a) => a);

        var dto = new HotelBookingAppWebApi.Models.DTOs.Amenity.CreateAmenityDto { Name = "Sauna", Category = "Services", IconName = "spa" };

        // Act
        var result = await _sut.CreateAmenityAsync(dto);

        // Assert
        result.Name.Should().Be("Sauna");
        result.IsActive.Should().BeTrue();
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAmenityAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var amenities = new List<Amenity> { MakeAmenity("Sauna") }.AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);

        var dto = new HotelBookingAppWebApi.Models.DTOs.Amenity.CreateAmenityDto { Name = "Sauna", Category = "Services" };

        // Act & Assert
        await _sut.Invoking(s => s.CreateAmenityAsync(dto))
            .Should().ThrowAsync<ConflictException>().WithMessage("*already exists*");
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAmenityAsync_OnException_Rollback()
    {
        // Arrange
        var amenities = new List<Amenity>().AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);
        _amenityRepo.Setup(r => r.AddAsync(It.IsAny<Amenity>())).ThrowsAsync(new Exception("db error"));

        var dto = new HotelBookingAppWebApi.Models.DTOs.Amenity.CreateAmenityDto { Name = "NewAmenity", Category = "Services" };

        // Act & Assert
        await _sut.Invoking(s => s.CreateAmenityAsync(dto)).Should().ThrowAsync<Exception>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── UpdateAmenityAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAmenityAsync_ExistingAmenity_UpdatesSuccessfully()
    {
        // Arrange
        var amenity = MakeAmenity("WiFi");
        _amenityRepo.Setup(r => r.GetAsync(amenity.AmenityId)).ReturnsAsync(amenity);
        _amenityRepo.Setup(r => r.UpdateAsync(amenity.AmenityId, It.IsAny<Amenity>())).ReturnsAsync(amenity);

        var dto = new HotelBookingAppWebApi.Models.DTOs.Amenity.UpdateAmenityDto
        {
            AmenityId = amenity.AmenityId,
            Name = "WiFi 6",
            Category = "Tech",
            IconName = "wifi",
            IsActive = true
        };

        // Act
        var result = await _sut.UpdateAmenityAsync(dto);

        // Assert
        result.Name.Should().Be("WiFi 6");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAmenityAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _amenityRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Amenity?)null);

        var dto = new HotelBookingAppWebApi.Models.DTOs.Amenity.UpdateAmenityDto
        {
            AmenityId = Guid.NewGuid(), Name = "X", Category = "Y", IsActive = true
        };

        // Act & Assert
        await _sut.Invoking(s => s.UpdateAmenityAsync(dto))
            .Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── GetAllAmenitiesPagedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllAmenitiesPagedAsync_NoFilter_ReturnsAll()
    {
        // Arrange
        var amenities = new List<Amenity>
        {
            MakeAmenity("WiFi"),
            MakeAmenity("Pool")
        }.AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);

        // Act
        var result = await _sut.GetAllAmenitiesPagedAsync(1, 10, null, null);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Amenities.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAmenitiesPagedAsync_WithSearchFilter_FiltersCorrectly()
    {
        // Arrange
        var amenities = new List<Amenity>
        {
            MakeAmenity("WiFi"),
            MakeAmenity("Pool")
        }.AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);

        // Act
        var result = await _sut.GetAllAmenitiesPagedAsync(1, 10, "wifi", null);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAmenitiesPagedAsync_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        var a1 = MakeAmenity("WiFi"); a1.Category = "Tech";
        var a2 = MakeAmenity("Pool"); a2.Category = "Services";
        var amenities = new List<Amenity> { a1, a2 }.AsQueryable().BuildMock();
        _amenityRepo.Setup(r => r.GetQueryable()).Returns(amenities);

        // Act
        var result = await _sut.GetAllAmenitiesPagedAsync(1, 10, null, "Tech");

        // Assert
        result.TotalCount.Should().Be(1);
        result.Amenities.First().Category.Should().Be("Tech");
    }

    // ── ToggleAmenityStatusAsync ──────────────────────────────────────────────

    [Fact]
    public async Task ToggleAmenityStatusAsync_ActiveAmenity_DeactivatesIt()
    {
        // Arrange
        var amenity = MakeAmenity(isActive: true);
        _amenityRepo.Setup(r => r.GetAsync(amenity.AmenityId)).ReturnsAsync(amenity);
        _amenityRepo.Setup(r => r.UpdateAsync(amenity.AmenityId, It.IsAny<Amenity>())).ReturnsAsync(amenity);

        // Act
        var result = await _sut.ToggleAmenityStatusAsync(amenity.AmenityId);

        // Assert
        result.Should().BeFalse();
        amenity.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleAmenityStatusAsync_InactiveAmenity_ActivatesIt()
    {
        // Arrange
        var amenity = MakeAmenity(isActive: false);
        _amenityRepo.Setup(r => r.GetAsync(amenity.AmenityId)).ReturnsAsync(amenity);
        _amenityRepo.Setup(r => r.UpdateAsync(amenity.AmenityId, It.IsAny<Amenity>())).ReturnsAsync(amenity);

        // Act
        var result = await _sut.ToggleAmenityStatusAsync(amenity.AmenityId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleAmenityStatusAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _amenityRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Amenity?)null);

        // Act & Assert
        await _sut.Invoking(s => s.ToggleAmenityStatusAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── DeleteAmenityAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAmenityAsync_NotInUse_DeletesSuccessfully()
    {
        // Arrange
        var amenity = MakeAmenity();
        _amenityRepo.Setup(r => r.GetAsync(amenity.AmenityId)).ReturnsAsync(amenity);
        _amenityRepo.Setup(r => r.DeleteAsync(amenity.AmenityId)).ReturnsAsync(amenity);
        // InMemory context has no RoomTypeAmenities — so inUse = false

        // Act
        var result = await _sut.DeleteAmenityAsync(amenity.AmenityId);

        // Assert
        result.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAmenityAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _amenityRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Amenity?)null);

        // Act & Assert
        await _sut.Invoking(s => s.DeleteAmenityAsync(Guid.NewGuid()))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAmenityAsync_InUse_ThrowsConflictException()
    {
        // Arrange
        var amenity = MakeAmenity();
        _amenityRepo.Setup(r => r.GetAsync(amenity.AmenityId)).ReturnsAsync(amenity);

        // Seed a RoomTypeAmenity in the InMemory context
        _context.RoomTypeAmenities.Add(new RoomTypeAmenity { RoomTypeId = Guid.NewGuid(), AmenityId = amenity.AmenityId });
        await _context.SaveChangesAsync();

        // Act & Assert
        await _sut.Invoking(s => s.DeleteAmenityAsync(amenity.AmenityId))
            .Should().ThrowAsync<ConflictException>().WithMessage("*in use*");
    }
}
