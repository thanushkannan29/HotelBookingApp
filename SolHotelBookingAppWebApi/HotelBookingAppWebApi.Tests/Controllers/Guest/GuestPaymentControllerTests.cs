using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Guest;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Guest;

public class GuestPaymentControllerTests
{
    private readonly Mock<IReservationService> _serviceMock = new();
    private readonly GuestPaymentController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public GuestPaymentControllerTests()
    {
        _sut = new GuestPaymentController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task GetQrCode_ValidReservationId_ReturnsOk()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetPaymentQrAsync(_userId, reservationId)).ReturnsAsync(new QrPaymentResponseDto());

        // Act
        var result = await _sut.GetQrCode(reservationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetQrCode_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetPaymentQrAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException("Reservation not found."));

        // Act
        var act = async () => await _sut.GetQrCode(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
