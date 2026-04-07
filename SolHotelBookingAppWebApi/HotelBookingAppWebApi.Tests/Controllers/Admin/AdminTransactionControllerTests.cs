using FluentAssertions;
using HotelBookingAppWebApi.Controllers.Admin;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers.Admin;

public class AdminTransactionControllerTests
{
    private readonly Mock<ITransactionService> _serviceMock = new();
    private readonly AdminTransactionController _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AdminTransactionControllerTests()
    {
        _sut = new AdminTransactionController(_serviceMock.Object);
        _sut.ControllerContext = ControllerTestHelper.BuildControllerContext(_userId, "Admin");
    }

    [Fact]
    public async Task MarkFailed_ValidTransactionId_ReturnsOk()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        _serviceMock.Setup(s => s.MarkTransactionFailedAsync(transactionId, _userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.MarkFailed(transactionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkFailed_ServiceThrows_PropagatesException()
    {
        // Arrange
        _serviceMock.Setup(s => s.MarkTransactionFailedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException("Transaction not found."));

        // Act
        var act = async () => await _sut.MarkFailed(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
