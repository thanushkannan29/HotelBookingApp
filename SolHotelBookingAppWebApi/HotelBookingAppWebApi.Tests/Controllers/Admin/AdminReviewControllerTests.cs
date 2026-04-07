using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminReviewControllerTests
{
    private readonly Mock<IReviewService> _serviceMock = new();
    private readonly AdminReviewController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminReviewControllerTests()
    {
        _sut = new AdminReviewController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task GetHotelReviews_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new GetHotelReviewsRequestDto { HotelId = Guid.NewGuid(), Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAdminHotelReviewsAsync(_userId, 1, 10, null, null, null))
            .ReturnsAsync(new PagedReviewResponseDto());

        // Act
        var result = await _sut.GetHotelReviews(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetHotelReviews_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAdminHotelReviewsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>()))
            .ThrowsAsync(new NotFoundException("Hotel not found."));

        // Act
        var act = async () => await _sut.GetHotelReviews(new GetHotelReviewsRequestDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Reply_ValidRequest_ReturnsOk()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var dto = new ReplyToReviewDto { AdminReply = "Thank you for your feedback." };
        _serviceMock.Setup(s => s.ReplyToReviewAsync(_userId, reviewId, dto.AdminReply)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Reply(reviewId, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Reply_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.ReplyToReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new NotFoundException("Review not found."));

        // Act
        var act = async () => await _sut.Reply(Guid.NewGuid(), new ReplyToReviewDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
