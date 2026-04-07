using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.SuperAdmin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.SuperAdmin;

public class SuperAdminSupportControllerTests
{
    private readonly Mock<ISupportRequestService> _serviceMock = new();
    private readonly SuperAdminSupportController _sut;

    public SuperAdminSupportControllerTests()
        => _sut = new SuperAdminSupportController(_serviceMock.Object);

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new SupportQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAllRequestsAsync(null, null, null, 1, 10))
            .ReturnsAsync(new PagedSupportRequestResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Respond_ValidRequest_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RespondSupportRequestDto { Response = "We'll fix it.", Status = "Resolved" };
        _serviceMock.Setup(s => s.RespondAsync(id, dto)).ReturnsAsync(new SupportRequestResponseDto());

        // Act
        var result = await _sut.Respond(id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Respond_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.RespondAsync(It.IsAny<Guid>(), It.IsAny<RespondSupportRequestDto>()))
            .ThrowsAsync(new NotFoundException("Request not found."));

        // Act
        var act = async () => await _sut.Respond(Guid.NewGuid(), new RespondSupportRequestDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
