using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Guest;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.PromoCode;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Guest;

public class GuestPromoCodeControllerTests
{
    private readonly Mock<IPromoCodeService> _serviceMock = new();
    private readonly GuestPromoCodeController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public GuestPromoCodeControllerTests()
    {
        _sut = new GuestPromoCodeController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PromoQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetGuestPromoCodesPagedAsync(_userId, 1, 10, null))
            .ReturnsAsync(new PagedPromoCodeResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Validate_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new ValidatePromoCodeDto { Code = "SAVE10", HotelId = Guid.NewGuid(), TotalAmount = 1000 };
        _serviceMock.Setup(s => s.ValidateAsync(_userId, dto)).ReturnsAsync(new PromoCodeValidationResultDto());

        // Act
        var result = await _sut.Validate(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Validate_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.ValidateAsync(It.IsAny<Guid>(), It.IsAny<ValidatePromoCodeDto>()))
            .ThrowsAsync(new ValidationException("Invalid promo code."));

        // Act
        var act = async () => await _sut.Validate(new ValidatePromoCodeDto());

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
