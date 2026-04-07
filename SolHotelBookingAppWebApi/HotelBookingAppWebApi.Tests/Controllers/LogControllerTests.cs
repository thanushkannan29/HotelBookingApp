using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Log;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers;

public class LogControllerTests
{
    private readonly Mock<ILogService> _serviceMock = new();
    private readonly LogController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public LogControllerTests()
    {
        _sut = new LogController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task GetMyLogs_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PageQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetUserLogsAsync(_userId, 1, 10)).ReturnsAsync(new PagedLogResponseDto());

        // Act
        var result = await _sut.GetMyLogs(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyLogs_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUserLogsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.GetMyLogs(new PageQueryDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetAll_ValidQuery_ReturnsOk()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "SuperAdmin");
        var dto = new LogQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAllLogsAsync(1, 10, null)).ReturnsAsync(new PagedLogResponseDto());

        // Act
        var result = await _sut.GetAll(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
