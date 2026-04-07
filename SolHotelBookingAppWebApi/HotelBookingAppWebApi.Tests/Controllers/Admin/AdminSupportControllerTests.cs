using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminSupportControllerTests
{
    private readonly Mock<ISupportRequestService> _serviceMock = new();
    private readonly AdminSupportController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminSupportControllerTests()
    {
        _sut = new AdminSupportController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task Submit_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new AdminSupportRequestDto { Subject = "Bug", Message = "Found a bug", Category = "Technical" };
        _serviceMock.Setup(s => s.CreateAdminRequestAsync(_userId, dto))
            .ReturnsAsync(new SupportRequestResponseDto());

        // Act
        var result = await _sut.Submit(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Submit_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateAdminRequestAsync(It.IsAny<Guid>(), It.IsAny<AdminSupportRequestDto>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.Submit(new AdminSupportRequestDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PageQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAdminRequestsAsync(_userId, 1, 10))
            .ReturnsAsync(new PagedSupportRequestResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
