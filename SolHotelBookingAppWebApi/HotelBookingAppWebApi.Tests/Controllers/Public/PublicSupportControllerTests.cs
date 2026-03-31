using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Public;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.SupportRequest;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Public;

public class PublicSupportControllerTests
{
    private readonly Mock<ISupportRequestService> _serviceMock = new();
    private readonly PublicSupportController _sut;

    public PublicSupportControllerTests()
        => _sut = new PublicSupportController(_serviceMock.Object);

    [Fact]
    public async Task Submit_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new PublicSupportRequestDto
        {
            Name = "John", Email = "john@test.com",
            Subject = "Help", Message = "Need help", Category = "General"
        };
        _serviceMock.Setup(s => s.CreatePublicRequestAsync(dto)).ReturnsAsync(new SupportRequestResponseDto());

        // Act
        var result = await _sut.Submit(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Submit_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreatePublicRequestAsync(It.IsAny<PublicSupportRequestDto>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.Submit(new PublicSupportRequestDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
