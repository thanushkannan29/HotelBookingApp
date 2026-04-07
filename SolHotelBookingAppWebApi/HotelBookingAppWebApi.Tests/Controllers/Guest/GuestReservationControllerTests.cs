using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Controllers.Guest;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Guest;

public class GuestReservationControllerTests
{
    private readonly Mock<IReservationService> _serviceMock = new();
    private readonly GuestReservationController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public GuestReservationControllerTests()
    {
        _sut = new GuestReservationController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreateReservationDto { HotelId = Guid.NewGuid(), RoomTypeId = Guid.NewGuid(), NumberOfRooms = 1 };
        _serviceMock.Setup(s => s.CreateReservationAsync(_userId, dto)).ReturnsAsync(new ReservationResponseDto());

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreateReservationAsync(It.IsAny<Guid>(), It.IsAny<CreateReservationDto>()))
            .ThrowsAsync(new InsufficientInventoryException("No rooms available."));

        // Act
        var act = async () => await _sut.Create(new CreateReservationDto());

        // Assert
        await act.Should().ThrowAsync<InsufficientInventoryException>();
    }

    [Fact]
    public async Task GetByCode_ValidCode_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetReservationByCodeAsync(_userId, "RES001")).ReturnsAsync(new ReservationDetailsDto());

        // Act
        var result = await _sut.GetByCode("RES001");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ValidRequest_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetMyReservationsAsync(_userId))
            .ReturnsAsync(new List<ReservationDetailsDto>());

        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetHistory_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new ReservationHistoryQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetMyReservationsPagedAsync(_userId, 1, 10, null, null))
            .ReturnsAsync(new PagedReservationResponseDto());

        // Act
        var result = await _sut.GetHistory(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Cancel_ValidRequest_ReturnsOk()
    {
        // Arrange
        var dto = new CancelReservationDto { Reason = "Changed plans" };
        _serviceMock.Setup(s => s.CancelReservationAsync(_userId, "RES001", "Changed plans")).ReturnsAsync(true);

        // Act
        var result = await _sut.Cancel("RES001", dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailableRooms_ValidQuery_ReturnsOk()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        _serviceMock.Setup(s => s.GetAvailableRoomsAsync(hotelId, roomTypeId, checkIn, checkOut))
            .ReturnsAsync(new List<AvailableRoomDto>());

        // Act
        var result = await _sut.GetAvailableRooms(hotelId, roomTypeId, checkIn, checkOut);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
