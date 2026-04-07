using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Public;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Public;

public class PublicHotelControllerTests
{
    private readonly Mock<IHotelService> _serviceMock = new();
    private readonly PublicHotelController _sut;

    public PublicHotelControllerTests()
        => _sut = new PublicHotelController(_serviceMock.Object);

    [Fact]
    public async Task GetTopHotels_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetTopHotelsAsync()).ReturnsAsync(new List<HotelListItemDto>());

        // Act
        var result = await _sut.GetTopHotels();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Search_ValidRequest_ReturnsOk()
    {
        // Arrange
        var dto = new SearchHotelRequestDto { City = "Mumbai" };
        _serviceMock.Setup(s => s.SearchHotelsAsync(dto)).ReturnsAsync(new SearchHotelResponseDto());

        // Act
        var result = await _sut.Search(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCities_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetCitiesAsync()).ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.GetCities();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCity_ValidCity_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetHotelsByCityAsync("Mumbai")).ReturnsAsync(new List<HotelListItemDto>());

        // Act
        var result = await _sut.GetByCity("Mumbai");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFullDetails_ValidHotelId_ReturnsOk()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetHotelDetailsAsync(hotelId)).ReturnsAsync(new HotelDetailsDto());

        // Act
        var result = await _sut.GetFullDetails(hotelId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDetails_ValidHotelId_ReturnsOk()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetHotelDetailsAsync(hotelId)).ReturnsAsync(new HotelDetailsDto());

        // Act
        var result = await _sut.GetDetails(hotelId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRoomTypes_ValidHotelId_ReturnsOk()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetRoomTypesAsync(hotelId)).ReturnsAsync(new List<RoomTypePublicDto>());

        // Act
        var result = await _sut.GetRoomTypes(hotelId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailability_ValidRequest_ReturnsOk()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        _serviceMock.Setup(s => s.GetAvailabilityAsync(hotelId, checkIn, checkOut))
            .ReturnsAsync(new List<RoomAvailabilityDto>());

        // Act
        var result = await _sut.GetAvailability(hotelId, checkIn, checkOut);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActiveStates_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetActiveStatesAsync()).ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.GetActiveStates();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByState_ValidState_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetHotelsByStateAsync("Maharashtra")).ReturnsAsync(new List<HotelListItemDto>());

        // Act
        var result = await _sut.GetByState("Maharashtra");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
