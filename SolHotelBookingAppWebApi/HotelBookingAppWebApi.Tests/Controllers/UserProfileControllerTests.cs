using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers;

public class UserProfileControllerTests
{
    private readonly Mock<IUserService> _serviceMock = new();
    private readonly UserProfileController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public UserProfileControllerTests()
    {
        _sut = new UserProfileController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task GetProfile_ValidRequest_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetProfileAsync(_userId)).ReturnsAsync(new UserProfileResponseDto());

        // Act
        var result = await _sut.GetProfile();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProfile_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetProfileAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new UserProfileException("Profile not found."));

        // Act
        var act = async () => await _sut.GetProfile();

        // Assert
        await act.Should().ThrowAsync<UserProfileException>();
    }

    [Fact]
    public async Task UpdateProfile_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateUserProfileDto { Name = "Updated Name" };
        _serviceMock.Setup(s => s.UpdateProfileAsync(_userId, dto)).ReturnsAsync(new UserProfileResponseDto());

        // Act
        var result = await _sut.UpdateProfile(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBookingHistory_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PaginationDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetBookingHistoryAsync(_userId, 1, 10)).ReturnsAsync(new PagedBookingHistoryDto());

        // Act
        var result = await _sut.GetBookingHistory(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
