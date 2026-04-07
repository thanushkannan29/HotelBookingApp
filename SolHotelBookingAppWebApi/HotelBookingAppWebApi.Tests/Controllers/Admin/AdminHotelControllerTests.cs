using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminHotelControllerTests
{
    private readonly Mock<IHotelService> _hotelServiceMock = new();
    private readonly AdminHotelController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminHotelControllerTests()
    {
        _sut = new AdminHotelController(_hotelServiceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task Update_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateHotelDto { Name = "Updated Hotel" };
        _hotelServiceMock.Setup(s => s.UpdateHotelAsync(_userId, dto)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Update(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_ServiceThrows_PropagatesException()
    {
        // Arrange
        _hotelServiceMock.Setup(s => s.UpdateHotelAsync(It.IsAny<Guid>(), It.IsAny<UpdateHotelDto>()))
            .ThrowsAsync(new NotFoundException("Hotel not found."));

        // Act
        var act = async () => await _sut.Update(new UpdateHotelDto());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ToggleStatus_ValidRequest_ReturnsOk()
    {
        // Arrange
        _hotelServiceMock.Setup(s => s.ToggleHotelStatusAsync(_userId, true)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ToggleStatus(true);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ToggleStatus_ServiceThrows_PropagatesException()
    {
        // Arrange
        _hotelServiceMock.Setup(s => s.ToggleHotelStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ThrowsAsync(new ValidationException("Hotel is blocked."));

        // Act
        var act = async () => await _sut.ToggleStatus(true);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateGst_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateHotelGstDto { GstPercent = 18 };
        _hotelServiceMock.Setup(s => s.UpdateHotelGstAsync(_userId, 18)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateGst(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateGst_ServiceThrows_PropagatesException()
    {
        // Arrange
        _hotelServiceMock.Setup(s => s.UpdateHotelGstAsync(It.IsAny<Guid>(), It.IsAny<decimal>()))
            .ThrowsAsync(new ValidationException("Invalid GST."));

        // Act
        var act = async () => await _sut.UpdateGst(new UpdateHotelGstDto());

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }
}
