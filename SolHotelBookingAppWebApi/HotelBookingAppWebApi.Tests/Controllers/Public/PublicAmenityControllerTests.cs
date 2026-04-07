using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Public;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Public;

public class PublicAmenityControllerTests
{
    private readonly Mock<IAmenityService> _serviceMock = new();
    private readonly PublicAmenityController _sut;

    public PublicAmenityControllerTests()
        => _sut = new PublicAmenityController(_serviceMock.Object);

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllActiveAsync()).ReturnsAsync(new List<AmenityResponseDto>());

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllActiveAsync()).ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.GetAll();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Search_ValidQuery_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.SearchAsync("wifi")).ReturnsAsync(new List<AmenityResponseDto>());

        // Act
        var result = await _sut.Search("wifi");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
