using HotelBookingAppWebApi.Services;

namespace HotelBookingAppWebApi.Tests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    // ── HashPassword — new salt ───────────────────────────────────────────────

    [Fact]
    public void HashPassword_NewPassword_ReturnsHashAndSalt()
    {
        // Arrange
        var password = "SecurePass123";

        // Act
        var hash = _sut.HashPassword(password, null, out var salt);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashesWithDifferentSalts()
    {
        // Arrange
        var password = "SamePassword";

        // Act
        var hash1 = _sut.HashPassword(password, null, out var salt1);
        var hash2 = _sut.HashPassword(password, null, out var salt2);

        // Assert
        // Different salts → different hashes
        salt1.Should().NotBeEquivalentTo(salt2);
        hash1.Should().NotBeEquivalentTo(hash2);
    }

    [Fact]
    public void HashPassword_WithExistingSalt_ProducesSameHash()
    {
        // Arrange
        var password = "ConsistentPass";
        var original = _sut.HashPassword(password, null, out var salt);

        // Act — re-hash with the same salt
        var rehashed = _sut.HashPassword(password, salt, out var outSalt);

        // Assert
        rehashed.Should().BeEquivalentTo(original);
        outSalt.Should().BeNull(); // no new salt generated when existing salt provided
    }

    [Fact]
    public void HashPassword_WithExistingSalt_OutSaltIsNull()
    {
        // Arrange
        var password = "TestPass";
        _sut.HashPassword(password, null, out var salt);

        // Act
        _sut.HashPassword(password, salt, out var outSalt);

        // Assert
        outSalt.Should().BeNull();
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var act = () => _sut.HashPassword("", null, out _);
        act.Should().Throw<ArgumentException>().WithMessage("*Password*");
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var act = () => _sut.HashPassword(null!, null, out _);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HashPassword_WrongSalt_ProducesDifferentHash()
    {
        // Arrange
        var password = "MyPassword";
        var original = _sut.HashPassword(password, null, out var correctSalt);
        var wrongSalt = new byte[32]; // all zeros

        // Act
        var wrongHash = _sut.HashPassword(password, wrongSalt, out _);

        // Assert
        wrongHash.Should().NotBeEquivalentTo(original);
    }

    [Fact]
    public void HashPassword_DifferentPasswords_SameSalt_ProduceDifferentHashes()
    {
        // Arrange
        var pass1 = "Password1";
        var pass2 = "Password2";
        _sut.HashPassword(pass1, null, out var salt);

        // Act
        var hash1 = _sut.HashPassword(pass1, salt, out _);
        var hash2 = _sut.HashPassword(pass2, salt, out _);

        // Assert
        hash1.Should().NotBeEquivalentTo(hash2);
    }
}
