using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Room;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminRoomControllerTests
{
    private readonly Mock<IRoomService> _roomServiceMock = new();
    private readonly Mock<IReservationService> _reservationServiceMock = new();
    private readonly AdminRoomController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminRoomControllerTests()
    {
        _sut = new AdminRoomController(_roomServiceMock.Object, _reservationServiceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task Add_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = Guid.NewGuid() };
        _roomServiceMock.Setup(s => s.AddRoomAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Add(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Add_ServiceThrows_PropagatesException()
    {
        // Arrange
        _roomServiceMock.Setup(s => s.AddRoomAsync(It.IsAny<Guid>(), It.IsAny<CreateRoomDto>()))
            .ThrowsAsync(new ConflictException("Room number already exists."));

        // Act
        var act = async () => await _sut.Add(new CreateRoomDto());

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Update_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateRoomDto { RoomId = Guid.NewGuid(), RoomNumber = "102" };
        _roomServiceMock.Setup(s => s.UpdateRoomAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Update(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ToggleStatus_ValidRequest_ReturnsOk()
    {
        // Arrange
        var roomId = Guid.NewGuid();
        _roomServiceMock.Setup(s => s.ToggleRoomStatusAsync(_userId, roomId, true)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ToggleStatus(roomId, true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PageQueryDto { Page = 1, PageSize = 10 };
        _roomServiceMock.Setup(s => s.GetRoomsByHotelAsync(_userId, 1, 10))
            .ReturnsAsync(new List<RoomListResponseDto>());
        _roomServiceMock.Setup(s => s.GetRoomCountByHotelAsync(_userId)).ReturnsAsync(5);

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOccupancy_ValidDate_ReturnsOk()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        _reservationServiceMock.Setup(s => s.GetRoomOccupancyAsync(_userId, date))
            .ReturnsAsync(new List<RoomOccupancyDto>());

        // Act
        var result = await _sut.GetOccupancy(date);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
