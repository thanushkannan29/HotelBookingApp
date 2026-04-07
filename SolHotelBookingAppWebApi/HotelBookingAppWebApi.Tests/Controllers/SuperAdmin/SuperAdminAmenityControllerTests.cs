using FluentAssertions;
using HotelBookingAppWebApi.Controllers.SuperAdmin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.SuperAdmin;

public class SuperAdminAmenityControllerTests
{
    private readonly Mock<IAmenityService> _serviceMock = new();
    private readonly SuperAdminAmenityController _sut;

    public SuperAdminAmenityControllerTests()
        => _sut = new SuperAdminAmenityController(_serviceMock.Object);

    [Fact]
    public async Task Create_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateAmenityDto { Name = "Sauna", Category = "Services" };
        _serviceMock.Setup(s => s.CreateAmenityAsync(dto)).ReturnsAsync(new AmenityResponseDto());

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateAmenityAsync(It.IsAny<CreateAmenityDto>()))
            .ThrowsAsync(new ConflictException("Amenity already exists."));

        // Act
        var act = async () => await _sut.Create(new CreateAmenityDto());

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateAmenityDto { AmenityId = Guid.NewGuid(), Name = "Updated" };
        _serviceMock.Setup(s => s.UpdateAmenityAsync(dto)).ReturnsAsync(new AmenityResponseDto());

        // Act
        var result = await _sut.Update(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ValidQuery_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllAmenitiesPagedAsync(1, 10, null, null)).ReturnsAsync(new PagedAmenityResponseDto());

        // Act
        var result = await _sut.GetAll(1, 10, null, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ToggleStatus_ValidId_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.ToggleAmenityStatusAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _sut.ToggleStatus(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ValidId_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAmenityAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAmenityAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new ValidationException("Amenity is in use."));

        // Act
        var act = async () => await _sut.Delete(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
