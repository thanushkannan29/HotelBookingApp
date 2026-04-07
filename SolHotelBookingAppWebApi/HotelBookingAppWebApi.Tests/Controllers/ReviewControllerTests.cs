using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Review;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers;

public class ReviewControllerTests
{
    private readonly Mock<IReviewService> _serviceMock = new();
    private readonly ReviewController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public ReviewControllerTests()
    {
        _sut = new ReviewController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task AddReview_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateReviewDto { HotelId = Guid.NewGuid(), ReservationId = Guid.NewGuid(), Rating = 4, Comment = "Great!" };
        _serviceMock.Setup(s => s.AddReviewAsync(_userId, dto)).ReturnsAsync(new ReviewResponseDto());

        // Act
        var result = await _sut.AddReview(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddReview_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.AddReviewAsync(It.IsAny<Guid>(), It.IsAny<CreateReviewDto>()))
            .ThrowsAsync(new ReviewException("Already reviewed."));

        // Act
        var act = async () => await _sut.AddReview(new CreateReviewDto());

        // Assert
        await act.Should().ThrowAsync<ReviewException>();
    }

    [Fact]
    public async Task UpdateReview_ValidRequest_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new UpdateReviewDto { Rating = 5, Comment = "Excellent!" };
        _serviceMock.Setup(s => s.UpdateReviewAsync(_userId, id, dto)).ReturnsAsync(new ReviewResponseDto());

        // Act
        var result = await _sut.UpdateReview(id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteReview_ValidId_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteReviewAsync(_userId, id)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteReview(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteReview_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteReviewAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException("Review not found."));

        // Act
        var act = async () => await _sut.DeleteReview(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByHotel_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new GetHotelReviewsRequestDto { HotelId = Guid.NewGuid(), Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetReviewsByHotelAsync(dto.HotelId, 1, 10)).ReturnsAsync(new PagedReviewResponseDto());

        // Act
        var result = await _sut.GetByHotel(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyReviewsPaged_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PageQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetMyReviewsPagedAsync(_userId, 1, 10)).ReturnsAsync(new PagedMyReviewsResponseDto());

        // Act
        var result = await _sut.GetMyReviewsPaged(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
