using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;

namespace HotelBookingAppWebApi.Tests.Exceptions;

public class AppExceptionsTests
{
    [Fact]
    public void AppException_WithMessageAndStatusCode_SetsProperties()
    {
        // Arrange & Act
        var ex = new AppException("base error", 422);

        // Assert
        ex.StatusCode.Should().Be(422);
        ex.Message.Should().Be("base error");
    }

    [Fact]
    public void NotFoundException_WithMessage_Returns404()
    {
        // Arrange
        var message = "Hotel not found.";

        // Act
        var ex = new NotFoundException(message);

        // Assert
        ex.StatusCode.Should().Be(404);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ConflictException_WithMessage_Returns409()
    {
        // Arrange
        var message = "Email already registered.";

        // Act
        var ex = new ConflictException(message);

        // Assert
        ex.StatusCode.Should().Be(409);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ValidationException_WithMessage_Returns400()
    {
        // Arrange
        var message = "Invalid input.";

        // Act
        var ex = new ValidationException(message);

        // Assert
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void UnAuthorizedException_Default_Returns401WithDefaultMessage()
    {
        // Arrange & Act
        var ex = new UnAuthorizedException();

        // Assert
        ex.StatusCode.Should().Be(401);
        ex.Message.Should().Be("Unauthorized");
    }

    [Fact]
    public void UnAuthorizedException_CustomMessage_Returns401WithCustomMessage()
    {
        // Arrange
        var message = "Invalid credentials.";

        // Act
        var ex = new UnAuthorizedException(message);

        // Assert
        ex.StatusCode.Should().Be(401);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void PaymentException_WithMessage_Returns400()
    {
        // Arrange
        var message = "Payment failed.";

        // Act
        var ex = new PaymentException(message);

        // Assert
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ReservationFailedException_WithMessage_AppendsAndReturns400()
    {
        // Arrange
        var message = "Room unavailable";

        // Act
        var ex = new ReservationFailedException(message);

        // Assert
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Contain(message);
        ex.Message.Should().Contain("Reservation failed");
    }

    [Fact]
    public void InsufficientInventoryException_WithMessage_AppendsAndReturns409()
    {
        // Arrange
        var message = "No rooms left";

        // Act
        var ex = new InsufficientInventoryException(message);

        // Assert
        ex.StatusCode.Should().Be(409);
        ex.Message.Should().Contain(message);
        ex.Message.Should().Contain("Inventory insufficient");
    }

    [Fact]
    public void RateNotFoundException_WithMessage_AppendsAndReturns404()
    {
        // Arrange
        var message = "No rate for date";

        // Act
        var ex = new RateNotFoundException(message);

        // Assert
        ex.StatusCode.Should().Be(404);
        ex.Message.Should().Contain(message);
        ex.Message.Should().Contain("Rate not found");
    }

    [Fact]
    public void ReviewException_WithMessage_Returns400()
    {
        // Arrange
        var message = "Already reviewed.";

        // Act
        var ex = new ReviewException(message);

        // Assert
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void UserProfileException_WithMessage_Returns404()
    {
        // Arrange
        var message = "Profile not found.";

        // Act
        var ex = new UserProfileException(message);

        // Assert
        ex.StatusCode.Should().Be(404);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void UnableToCreateEntityException_Default_Returns400WithDefaultMessage()
    {
        // Arrange & Act
        var ex = new UnableToCreateEntityException();

        // Assert
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Be("Unable to create entity");
    }

    [Fact]
    public void UnableToCreateEntityException_CustomMessage_Returns400WithCustomMessage()
    {
        // Arrange
        var message = "Could not create reservation.";

        // Act
        var ex = new UnableToCreateEntityException(message);

        // Assert
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Be(message);
    }
}
