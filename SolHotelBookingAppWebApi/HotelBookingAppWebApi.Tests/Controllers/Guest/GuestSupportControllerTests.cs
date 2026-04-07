using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Guest;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Guest;

public class GuestSupportControllerTests
{
    private readonly Mock<ISupportRequestService> _serviceMock = new();
    private readonly GuestSupportController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public GuestSupportControllerTests()
    {
        _sut = new GuestSupportController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task Submit_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new GuestSupportRequestDto { Subject = "Issue", Message = "Help me", Category = "Billing" };
        _serviceMock.Setup(s => s.CreateGuestRequestAsync(_userId, dto))
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
        _serviceMock.Setup(s => s.CreateGuestRequestAsync(It.IsAny<Guid>(), It.IsAny<GuestSupportRequestDto>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.Submit(new GuestSupportRequestDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PageQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetGuestRequestsAsync(_userId, 1, 10))
            .ReturnsAsync(new PagedSupportRequestResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
