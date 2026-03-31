using FluentAssertions;
using HotelBookingAppWebApi.Services;

namespace HotelBookingAppWebApi.Tests.Services;

public class PasswordServiceTests
{
    [Fact]
    public void HashPassword_NullSalt_GeneratesNewSaltAndReturnsHash()
    {
        // Arrange
        var sut = new PasswordService();

        // Act
        var hash = sut.HashPassword("mypassword", null, out var newSalt);

        // Assert
        hash.Should().NotBeEmpty();
        newSalt.Should().NotBeNull();
        newSalt.Should().NotBeEmpty();
    }

    [Fact]
    public void HashPassword_ExistingSalt_NewSaltIsNull()
    {
        // Arrange
        var sut = new PasswordService();
        sut.HashPassword("mypassword", null, out var salt);

        // Act
        sut.HashPassword("mypassword", salt, out var newSalt);

        // Assert
        newSalt.Should().BeNull();
    }

    [Fact]
    public void HashPassword_SamePasswordAndSalt_ProducesIdenticalHash()
    {
        // Arrange
        var sut = new PasswordService();
        var hash1 = sut.HashPassword("password123", null, out var salt);

        // Act
        var hash2 = sut.HashPassword("password123", salt, out _);

        // Assert
        hash1.Should().Equal(hash2);
    }

    [Fact]
    public void HashPassword_DifferentPasswords_ProduceDifferentHashes()
    {
        // Arrange
        var sut = new PasswordService();
        var hash1 = sut.HashPassword("password1", null, out var salt);

        // Act
        var hash2 = sut.HashPassword("password2", salt, out _);

        // Assert
        hash1.Should().NotEqual(hash2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    public void HashPassword_EmptyOrNullPassword_ThrowsArgumentException(string? password)
    {
        // Arrange
        var sut = new PasswordService();

        // Act
        var act = () => sut.HashPassword(password!, null, out _);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
