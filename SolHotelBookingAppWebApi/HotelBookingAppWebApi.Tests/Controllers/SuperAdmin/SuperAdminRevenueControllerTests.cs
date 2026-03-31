using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.SuperAdmin;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Revenue;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.SuperAdmin;

public class SuperAdminRevenueControllerTests
{
    private readonly Mock<ISuperAdminRevenueService> _serviceMock = new();
    private readonly SuperAdminRevenueController _sut;

    public SuperAdminRevenueControllerTests()
        => _sut = new SuperAdminRevenueController(_serviceMock.Object);

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new RevenueQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAllRevenueAsync(1, 10)).ReturnsAsync(new PagedRevenueResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSummary_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetSummaryAsync()).ReturnsAsync(new RevenueSummaryDto());

        // Act
        var result = await _sut.GetSummary();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSummary_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetSummaryAsync()).ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.GetSummary();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
