using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _serviceMock = new();
    private readonly DashboardController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public DashboardControllerTests()
    {
        _sut = new DashboardController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task AdminDashboard_ValidRequest_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAdminDashboardAsync(_userId)).ReturnsAsync(new AdminDashboardDto());

        // Act
        var result = await _sut.AdminDashboard();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminDashboard_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAdminDashboardAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.AdminDashboard();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GuestDashboard_ValidRequest_ReturnsOk()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
        _serviceMock.Setup(s => s.GetGuestDashboardAsync(_userId)).ReturnsAsync(new GuestDashboardDto());

        // Act
        var result = await _sut.GuestDashboard();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SuperAdminDashboard_ValidRequest_ReturnsOk()
    {
        // Arrange
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "SuperAdmin");
        _serviceMock.Setup(s => s.GetSuperAdminDashboardAsync()).ReturnsAsync(new SuperAdminDashboardDto());

        // Act
        var result = await _sut.SuperAdminDashboard();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
