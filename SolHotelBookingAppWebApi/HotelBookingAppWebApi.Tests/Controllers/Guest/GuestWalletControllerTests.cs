using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Guest;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Guest;

public class GuestWalletControllerTests
{
    private readonly Mock<IWalletService> _serviceMock = new();
    private readonly GuestWalletController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public GuestWalletControllerTests()
    {
        _sut = new GuestWalletController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task GetWallet_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new PageQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetWalletAsync(_userId, 1, 10)).ReturnsAsync(new PagedWalletTransactionDto());

        // Act
        var result = await _sut.GetWallet(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TopUp_ValidAmount_ReturnsOk()
    {
        // Arrange
        var dto = new TopUpWalletDto { Amount = 500 };
        _serviceMock.Setup(s => s.TopUpAsync(_userId, 500)).ReturnsAsync(new WalletResponseDto());

        // Act
        var result = await _sut.TopUp(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TopUp_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.TopUpAsync(It.IsAny<Guid>(), It.IsAny<decimal>()))
            .ThrowsAsync(new ValidationException("Amount must be positive."));

        // Act
        var act = async () => await _sut.TopUp(new TopUpWalletDto());

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
