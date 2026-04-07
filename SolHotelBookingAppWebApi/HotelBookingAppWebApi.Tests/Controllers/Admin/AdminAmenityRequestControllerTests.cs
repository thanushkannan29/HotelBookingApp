using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminAmenityRequestControllerTests
{
    private readonly Mock<IAmenityRequestService> _serviceMock = new();
    private readonly AdminAmenityRequestController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminAmenityRequestControllerTests()
    {
        _sut = new AdminAmenityRequestController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateAmenityRequestDto { AmenityName = "Sauna", Category = "Services" };
        _serviceMock.Setup(s => s.CreateRequestAsync(_userId, dto)).ReturnsAsync(new AmenityRequestResponseDto());

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateRequestAsync(It.IsAny<Guid>(), It.IsAny<CreateAmenityRequestDto>()))
            .ThrowsAsync(new ConflictException("Request already exists."));

        // Act
        var act = async () => await _sut.Create(new CreateAmenityRequestDto());

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new AmenityRequestAdminQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAdminRequestsPagedAsync(_userId, 1, 10, null))
            .ReturnsAsync(new PagedAmenityRequestResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetList_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAdminRequestsPagedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.GetList(new AmenityRequestAdminQueryDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
