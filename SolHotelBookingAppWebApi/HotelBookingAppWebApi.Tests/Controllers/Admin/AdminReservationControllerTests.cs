using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminReservationControllerTests
{
    private readonly Mock<IReservationService> _serviceMock = new();
    private readonly AdminReservationController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminReservationControllerTests()
    {
        _sut = new AdminReservationController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new ReservationQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAdminReservationsAsync(_userId, "All", null, 1, 10, null, null))
            .ReturnsAsync(new PagedReservationResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetList_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAdminReservationsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act
        var act = async () => await _sut.GetList(new ReservationQueryDto());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Complete_ValidCode_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.CompleteReservationAsync("RES001")).ReturnsAsync(true);

        // Act
        var result = await _sut.Complete("RES001");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Complete_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.CompleteReservationAsync(It.IsAny<string>()))
            .ThrowsAsync(new NotFoundException("Reservation not found."));

        // Act
        var act = async () => await _sut.Complete("INVALID");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Confirm_ValidCode_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.ConfirmReservationAsync("RES001")).ReturnsAsync(true);

        // Act
        var result = await _sut.Confirm("RES001");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Confirm_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.ConfirmReservationAsync(It.IsAny<string>()))
            .ThrowsAsync(new NotFoundException("Reservation not found."));

        // Act
        var act = async () => await _sut.Confirm("INVALID");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
