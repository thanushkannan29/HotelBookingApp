using FluentAssertions;
using HotelBookingAppWebApi.Services;

namespace HotelBookingAppWebApi.Tests.Services;

public class QrCodeHelperTests
{
    [Fact]
    public void GenerateQrCodeBase64_ValidContent_ReturnsNonEmptyBase64()
    {
        // Arrange
        var content = "upi://pay?pa=hotel@upi&am=500&tn=RES001";

        // Act
        var result = QrCodeHelper.GenerateQrCodeBase64(content);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateQrCodeBase64_ValidContent_ReturnsValidBase64()
    {
        // Arrange
        var content = "upi://pay?pa=hotel@upi&am=1000";

        // Act
        var result = QrCodeHelper.GenerateQrCodeBase64(content);

        // Assert
        var act = () => Convert.FromBase64String(result);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("upi://pay?pa=test@upi&am=100&tn=RES123")]
    public void GenerateQrCodeBase64_DifferentInputs_ReturnsBase64(string content)
    {
        // Arrange & Act
        var result = QrCodeHelper.GenerateQrCodeBase64(content);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }
}
