using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminAuditLogControllerTests
{
    private readonly Mock<IAuditLogService> _serviceMock = new();
    private readonly AdminAuditLogController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminAuditLogControllerTests()
    {
        _sut = new AdminAuditLogController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new AuditLogQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAdminAuditLogsAsync(_userId, 1, 10, null))
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
        _serviceMock.Setup(s => s.GetAdminAuditLogsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.GetList(new AuditLogQueryDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
