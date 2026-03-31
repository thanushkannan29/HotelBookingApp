using FluentAssertions;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Amenity;
using HotelBookingAppWebApi.Models.DTOs.AmenityRequest;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Models.DTOs.Hotel.SuperAdmin;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.RoomType;

namespace HotelBookingAppWebApi.Tests.Models;

public class DtoModelTests
{
    // ── Review DTOs ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateReviewDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new CreateReviewDto();

        // Assert
        dto.Comment.Should().BeEmpty();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void CreateReviewDto_SetProperties_RetainsValues()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        // Act
        var dto = new CreateReviewDto { HotelId = hotelId, ReservationId = reservationId, Rating = 4.5m, Comment = "Great!", ImageUrl = "img.jpg" };

        // Assert
        dto.HotelId.Should().Be(hotelId);
        dto.ReservationId.Should().Be(reservationId);
        dto.Rating.Should().Be(4.5m);
        dto.Comment.Should().Be("Great!");
        dto.ImageUrl.Should().Be("img.jpg");
    }

    [Fact]
    public void UpdateReviewDto_SetProperties_RetainsValues()
    {
        // Arrange & Act
        var dto = new UpdateReviewDto { Rating = 3m, Comment = "Updated", ImageUrl = "new.jpg" };

        // Assert
        dto.Rating.Should().Be(3m);
        dto.Comment.Should().Be("Updated");
        dto.ImageUrl.Should().Be("new.jpg");
    }

    [Fact]
    public void ReviewResponseDto_DefaultContributionPoints_Is100()
    {
        // Arrange & Act
        var dto = new ReviewResponseDto();

        // Assert
        dto.ContributionPoints.Should().Be(100);
        dto.UserName.Should().BeEmpty();
        dto.Comment.Should().BeEmpty();
        dto.ReservationCode.Should().BeEmpty();
        dto.AdminReply.Should().BeNull();
        dto.ImageUrl.Should().BeNull();
        dto.UserProfileImageUrl.Should().BeNull();
    }

    [Fact]
    public void ReviewResponseDto_SetAllProperties_RetainsValues()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new ReviewResponseDto
        {
            ReviewId = reviewId, HotelId = hotelId, UserId = userId,
            UserName = "John", ReservationId = reservationId, ReservationCode = "RES001",
            Rating = 5m, Comment = "Excellent", ImageUrl = "img.jpg",
            UserProfileImageUrl = "profile.jpg", CreatedDate = now,
            AdminReply = "Thank you!", ContributionPoints = 100
        };

        // Assert
        dto.ReviewId.Should().Be(reviewId);
        dto.UserName.Should().Be("John");
        dto.AdminReply.Should().Be("Thank you!");
    }

    [Fact]
    public void MyReviewsResponseDto_DefaultContributionPoints_Is100()
    {
        // Arrange & Act
        var dto = new MyReviewsResponseDto();

        // Assert
        dto.ContributionPoints.Should().Be(100);
        dto.HotelName.Should().BeEmpty();
        dto.ReservationCode.Should().BeEmpty();
        dto.Comment.Should().BeEmpty();
        dto.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void MyReviewsResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new MyReviewsResponseDto
        {
            ReviewId = reviewId, HotelId = hotelId, HotelName = "Grand Hotel",
            ReservationId = reservationId, ReservationCode = "RES002",
            Rating = 4m, Comment = "Nice", ImageUrl = "img.jpg",
            CreatedDate = now, ContributionPoints = 100
        };

        // Assert
        dto.HotelName.Should().Be("Grand Hotel");
        dto.ReservationCode.Should().Be("RES002");
    }

    [Fact]
    public void PagedMyReviewsResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedMyReviewsResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Reviews.Should().BeEmpty();
    }

    [Fact]
    public void PagedMyReviewsResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var reviews = new List<MyReviewsResponseDto> { new() { ReviewId = Guid.NewGuid(), Rating = 4m } };

        // Act
        var dto = new PagedMyReviewsResponseDto { TotalCount = 1, Reviews = reviews };

        // Assert
        dto.TotalCount.Should().Be(1);
        dto.Reviews.Should().HaveCount(1);
    }

    [Fact]
    public void PagedReviewResponseDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new PagedReviewResponseDto();

        // Assert
        dto.TotalCount.Should().Be(0);
        dto.Reviews.Should().BeEmpty();
    }

    [Fact]
    public void PagedReviewResponseDto_SetProperties_RetainsValues()
    {
        // Arrange
        var reviews = new List<ReviewResponseDto> { new() { ReviewId = Guid.NewGuid(), Rating = 5m } };

        // Act
        var dto = new PagedReviewResponseDto { TotalCount = 1, Reviews = reviews };

        // Assert
        dto.TotalCount.Should().Be(1);
        dto.Reviews.Should().HaveCount(1);
    }

    [Fact]
    public void GetHotelReviewsRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new GetHotelReviewsRequestDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.MinRating.Should().BeNull();
        dto.MaxRating.Should().BeNull();
        dto.SortDir.Should().BeNull();
    }

    [Fact]
    public void ReplyToReviewDto_SetProperty_RetainsValue()
    {
        // Arrange & Act
        var dto = new ReplyToReviewDto { AdminReply = "Thank you!" };

        // Assert
        dto.AdminReply.Should().Be("Thank you!");
    }
