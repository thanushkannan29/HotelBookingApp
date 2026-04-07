using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminWalletControllerTests
{
    private readonly Mock<IWalletService> _serviceMock = new();
    private readonly AdminWalletController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminWalletControllerTests()
    {
        _sut = new AdminWalletController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task GetGuestWallet_ValidGuestId_ReturnsOk()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetGuestWalletByAdminAsync(_userId, guestId)).ReturnsAsync(new WalletResponseDto());

        // Act
        var result = await _sut.GetGuestWallet(guestId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGuestWallet_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetGuestWalletByAdminAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException("Guest not found."));

        // Act
        var act = async () => await _sut.GetGuestWallet(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
