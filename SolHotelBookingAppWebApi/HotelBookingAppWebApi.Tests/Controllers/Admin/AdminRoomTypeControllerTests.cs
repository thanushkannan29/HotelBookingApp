using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminRoomTypeControllerTests
{
    private readonly Mock<IRoomTypeService> _serviceMock = new();
    private readonly AdminRoomTypeController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminRoomTypeControllerTests()
    {
        _sut = new AdminRoomTypeController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PageQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetRoomTypesByHotelPagedAsync(_userId, 1, 10))
            .ReturnsAsync(new PagedRoomTypeResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Add_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateRoomTypeDto { Name = "Deluxe", MaxOccupancy = 2 };
        _serviceMock.Setup(s => s.AddRoomTypeAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Add(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateRoomTypeDto { RoomTypeId = Guid.NewGuid(), Name = "Suite" };
        _serviceMock.Setup(s => s.UpdateRoomTypeAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Update(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ToggleStatus_ValidRequest_ReturnsOk()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        _serviceMock.Setup(s => s.ToggleRoomTypeStatusAsync(_userId, roomTypeId, true)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ToggleStatus(roomTypeId, true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddRate_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateRoomTypeRateDto { RoomTypeId = Guid.NewGuid(), Rate = 1500 };
        _serviceMock.Setup(s => s.AddRateAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AddRate(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateRate_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateRoomTypeRateDto { RoomTypeRateId = Guid.NewGuid(), Rate = 2000 };
        _serviceMock.Setup(s => s.UpdateRateAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateRate(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRateByDate_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new GetRateByDateRequestDto { RoomTypeId = Guid.NewGuid(), Date = DateOnly.FromDateTime(DateTime.Today) };
        _serviceMock.Setup(s => s.GetRateByDateAsync(_userId, dto)).ReturnsAsync(1500m);

        // Act
        var result = await _sut.GetRateByDate(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRates_ValidRoomTypeId_ReturnsOk()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetRatesAsync(_userId, roomTypeId))
            .ReturnsAsync(new List<RoomTypeRateDto>());

        // Act
        var result = await _sut.GetRates(roomTypeId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Add_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.AddRoomTypeAsync(It.IsAny<Guid>(), It.IsAny<CreateRoomTypeDto>()))
            .ThrowsAsync(new ConflictException("Room type already exists."));

        // Act
        var act = async () => await _sut.Add(new CreateRoomTypeDto());

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }
}
