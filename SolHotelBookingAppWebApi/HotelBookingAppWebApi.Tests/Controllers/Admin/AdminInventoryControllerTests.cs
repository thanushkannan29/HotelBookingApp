using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminInventoryControllerTests
{
    private readonly Mock<IInventoryService> _serviceMock = new();
    private readonly AdminInventoryController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminInventoryControllerTests()
    {
        _sut = new AdminInventoryController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task Add_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateInventoryDto { RoomTypeId = Guid.NewGuid(), TotalInventory = 10 };
        _serviceMock.Setup(s => s.AddInventoryAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Add(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Add_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.AddInventoryAsync(It.IsAny<Guid>(), It.IsAny<CreateInventoryDto>()))
            .ThrowsAsync(new ValidationException("Invalid date range."));

        // Act
        var act = async () => await _sut.Add(new CreateInventoryDto());

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Update_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateInventoryDto { RoomTypeInventoryId = Guid.NewGuid(), TotalInventory = 15 };
        _serviceMock.Setup(s => s.UpdateInventoryAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Update(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateInventoryAsync(It.IsAny<Guid>(), It.IsAny<UpdateInventoryDto>()))
            .ThrowsAsync(new NotFoundException("Inventory not found."));

        // Act
        var act = async () => await _sut.Update(new UpdateInventoryDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_ValidQuery_ReturnsOk()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        _serviceMock.Setup(s => s.GetInventoryAsync(_userId, roomTypeId, start, end))
            .ReturnsAsync(new List<InventoryResponseDto>());

        // Act
        var result = await _sut.Get(roomTypeId, start, end);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetInventoryAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ThrowsAsync(new NotFoundException("Room type not found."));

        // Act
        var act = async () => await _sut.Get(Guid.NewGuid(), DateOnly.MinValue, DateOnly.MaxValue);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
