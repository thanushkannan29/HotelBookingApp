using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.SuperAdmin;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.SuperAdmin;

public class SuperAdminAuditLogControllerTests
{
    private readonly Mock<IAuditLogService> _serviceMock = new();
    private readonly SuperAdminAuditLogController _sut;

    public SuperAdminAuditLogControllerTests()
        => _sut = new SuperAdminAuditLogController(_serviceMock.Object);

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new AuditLogSuperAdminQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAllAuditLogsAsync(1, 10, null, null, null, null, null))
            .ReturnsAsync(new PagedAuditLogResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetList_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllAuditLogsAsync(It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.GetList(new AuditLogSuperAdminQueryDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
