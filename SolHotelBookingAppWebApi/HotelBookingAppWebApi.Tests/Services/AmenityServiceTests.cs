using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class AmenityServiceTests
{
    private readonly Mock<IRepository<Guid, Amenity>> _amenityRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private HotelBookingContext _context = null!;

    private HotelBookingContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HotelBookingContext(options);
    }

    private AmenityService CreateSut(string dbName)
    {
        _context = CreateContext(dbName);
        return new AmenityService(_amenityRepoMock.Object, _context, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveAmenities()
    {
        // Arrange
        var amenities = new List<Amenity>
        {
            new() { AmenityId = Guid.NewGuid(), Name = "WiFi", Category = "Tech", IsActive = true },
            new() { AmenityId = Guid.NewGuid(), Name = "Pool", Category = "Services", IsActive = false }
        }.AsQueryable().BuildMock();
        _amenityRepoMock.Setup(r => r.GetQueryable()).Returns(amenities);
        var sut = CreateSut(nameof(GetAllActiveAsync_ReturnsOnlyActiveAmenities));

        // Act
        var result = await sut.GetAllActiveAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("WiFi");
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        // Arrange
        var sut = CreateSut(nameof(SearchAsync_EmptyQuery_ReturnsEmpty));

        // Act
        var result = await sut.SearchAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_MatchingQuery_ReturnsResults()
    {
        // Arrange
        var amenities = new List<Amenity>
        {
            new() { AmenityId = Guid.NewGuid(), Name = "WiFi", Category = "Tech", IsActive = true }
        }.AsQueryable().BuildMock();
        _amenityRepoMock.Setup(r => r.GetQueryable()).Returns(amenities);
        var sut = CreateSut(nameof(SearchAsync_MatchingQuery_ReturnsResults));

        // Act
        var result = await sut.SearchAsync("wifi");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAmenityAsync_ValidDto_ReturnsDto()
    {
        // Arrange
        var emptyQuery = new List<Amenity>().AsQueryable().BuildMock();
        _amenityRepoMock.Setup(r => r.GetQueryable()).Returns(emptyQuery);
        _amenityRepoMock.Setup(r => r.AddAsync(It.IsAny<Amenity>())).ReturnsAsync((Amenity a) => a);
        var sut = CreateSut(nameof(CreateAmenityAsync_ValidDto_ReturnsDto));
        var dto = new CreateAmenityDto { Name = "Sauna", Category = "Services" };

        // Act
        var result = await sut.CreateAmenityAsync(dto);

        // Assert
        result.Name.Should().Be("Sauna");
    }

    [Fact]
    public async Task CreateAmenityAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var existing = new List<Amenity>
        {
            new() { AmenityId = Guid.NewGuid(), Name = "Sauna", Category = "Services", IsActive = true }
        }.AsQueryable().BuildMock();
        _amenityRepoMock.Setup(r => r.GetQueryable()).Returns(existing);
        var sut = CreateSut(nameof(CreateAmenityAsync_DuplicateName_ThrowsConflictException));

        // Act
        var act = async () => await sut.CreateAmenityAsync(new CreateAmenityDto { Name = "Sauna", Category = "Services" });

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleAmenityStatusAsync_ExistingAmenity_TogglesStatus()
    {
        // Arrange
        var amenity = new Amenity { AmenityId = Guid.NewGuid(), Name = "WiFi", Category = "Tech", IsActive = true };
        _amenityRepoMock.Setup(r => r.GetAsync(amenity.AmenityId)).ReturnsAsync(amenity);
        _amenityRepoMock.Setup(r => r.UpdateAsync(amenity.AmenityId, amenity)).ReturnsAsync(amenity);
        var sut = CreateSut(nameof(ToggleAmenityStatusAsync_ExistingAmenity_TogglesStatus));

        // Act
        var result = await sut.ToggleAmenityStatusAsync(amenity.AmenityId);

        // Assert
        result.Should().BeFalse(); // was true, now false
    }

    [Fact]
    public async Task ToggleAmenityStatusAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _amenityRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Amenity?)null);
        var sut = CreateSut(nameof(ToggleAmenityStatusAsync_NotFound_ThrowsNotFoundException));

        // Act
        var act = async () => await sut.ToggleAmenityStatusAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAmenityAsync_NotInUse_ReturnsTrue()
    {
        // Arrange
        var amenityId = Guid.NewGuid();
        var amenity = new Amenity { AmenityId = amenityId, Name = "WiFi", Category = "Tech", IsActive = true };
        _amenityRepoMock.Setup(r => r.GetAsync(amenityId)).ReturnsAsync(amenity);
        _amenityRepoMock.Setup(r => r.DeleteAsync(amenityId)).ReturnsAsync(amenity);
        var sut = CreateSut(nameof(DeleteAmenityAsync_NotInUse_ReturnsTrue));

        // Act
        var result = await sut.DeleteAmenityAsync(amenityId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAmenityAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _amenityRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Amenity?)null);
        var sut = CreateSut(nameof(DeleteAmenityAsync_NotFound_ThrowsNotFoundException));

        // Act
        var act = async () => await sut.DeleteAmenityAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
