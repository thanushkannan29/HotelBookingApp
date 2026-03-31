using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.SuperAdmin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.SuperAdmin;

public class SuperAdminAmenityRequestControllerTests
{
    private readonly Mock<IAmenityRequestService> _serviceMock = new();
    private readonly SuperAdminAmenityRequestController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public SuperAdminAmenityRequestControllerTests()
    {
        _sut = new SuperAdminAmenityRequestController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "SuperAdmin");
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new AmenityRequestQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAllRequestsAsync("All", 1, 10)).ReturnsAsync(new PagedAmenityRequestResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Approve_ValidId_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.ApproveRequestAsync(id, _userId)).ReturnsAsync(new AmenityRequestResponseDto());

        // Act
        var result = await _sut.Approve(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Approve_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.ApproveRequestAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException("Request not found."));

        // Act
        var act = async () => await _sut.Approve(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Reject_ValidRequest_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RejectAmenityRequestDto { Note = "Not needed." };
        _serviceMock.Setup(s => s.RejectRequestAsync(id, _userId, dto.Note)).ReturnsAsync(new AmenityRequestResponseDto());

        // Act
        var result = await _sut.Reject(id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
