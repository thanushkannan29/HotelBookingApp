using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.SuperAdmin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Hotel.SuperAdmin;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.SuperAdmin;

public class SuperAdminHotelControllerTests
{
    private readonly Mock<IHotelService> _serviceMock = new();
    private readonly SuperAdminHotelController _sut;

    public SuperAdminHotelControllerTests()
        => _sut = new SuperAdminHotelController(_serviceMock.Object);

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new HotelQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAllHotelsForSuperAdminPagedAsync(1, 10, null, null))
            .ReturnsAsync(new PagedSuperAdminHotelResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Block_ValidId_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.BlockHotelAsync(id)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Block(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Block_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.BlockHotelAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException("Hotel not found."));

        // Act
        var act = async () => await _sut.Block(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Unblock_ValidId_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.UnblockHotelAsync(id)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Unblock(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
