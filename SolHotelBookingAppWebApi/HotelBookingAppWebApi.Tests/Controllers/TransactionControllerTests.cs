using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers;

public class TransactionControllerTests
{
    private readonly Mock<ITransactionService> _serviceMock = new();
    private readonly TransactionController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TransactionControllerTests()
    {
        _sut = new TransactionController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Guest");
    }

    [Fact]
    public async Task CreatePayment_ValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new CreatePaymentDto { ReservationId = Guid.NewGuid(), PaymentMethod = PaymentMethod.UPI };
        _serviceMock.Setup(s => s.CreatePaymentAsync(dto)).ReturnsAsync(new TransactionResponseDto());

        // Act
        var result = await _sut.CreatePayment(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreatePayment_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.CreatePaymentAsync(It.IsAny<CreatePaymentDto>()))
            .ThrowsAsync(new PaymentException("Payment failed."));

        // Act
        var act = async () => await _sut.CreatePayment(new CreatePaymentDto());

        // Assert
        await act.Should().ThrowAsync<PaymentException>();
    }

    [Fact]
    public async Task DirectRefund_ValidRequest_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RefundRequestDto { Reason = "Changed mind" };
        _serviceMock.Setup(s => s.DirectGuestRefundAsync(id, _userId, dto)).ReturnsAsync(new TransactionResponseDto());

        // Act
        var result = await _sut.DirectRefund(id, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RecordFailed_ValidReservationId_ReturnsOk()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        _serviceMock.Setup(s => s.RecordFailedPaymentAsync(reservationId, _userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RecordFailed(reservationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetList_ValidQuery_ReturnsOk()
    {
        // Arrange
        var dto = new TransactionQueryDto { Page = 1, PageSize = 10 };
        _serviceMock.Setup(s => s.GetAllTransactionsAsync(_userId, "Guest", 1, 10, null, null))
            .ReturnsAsync(new PagedTransactionResponseDto());

        // Act
        var result = await _sut.GetList(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPaymentIntent_ValidReservationId_ReturnsOk()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetPaymentIntentAsync(reservationId, _userId)).ReturnsAsync(new PaymentIntentDto());

        // Act
        var result = await _sut.GetPaymentIntent(reservationId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
