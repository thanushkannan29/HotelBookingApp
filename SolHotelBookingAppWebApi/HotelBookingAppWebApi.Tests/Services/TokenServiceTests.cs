using HotelBookingAppWebApi.Models.DTOs.Auth;
using HotelBookingAppWebApi.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private const string TestKey = "super-secret-jwt-key-for-testing-1234567890";

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Keys:Jwt"] = TestKey })
            .Build();
        _sut = new TokenService(config);
    }

    // ── CreateToken ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_ValidPayload_ReturnsNonEmptyToken()
    {
        // Arrange
        var payload = new TokenPayloadDto
        {
            UserId = Guid.NewGuid(),
            UserName = "Alice",
            Role = "Guest",
            HotelId = null
        };

        // Act
        var token = _sut.CreateToken(payload);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateToken_ValidPayload_TokenContainsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payload = new TokenPayloadDto { UserId = userId, UserName = "Bob", Role = "Admin" };

        // Act
        var token = _sut.CreateToken(payload);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert — JWT short claim type for NameIdentifier is "nameid"
        jwt.Claims.Should().Contain(c =>
            c.Value == userId.ToString());
    }

    [Fact]
    public void CreateToken_ValidPayload_TokenContainsRole()
    {
        // Arrange
        var payload = new TokenPayloadDto { UserId = Guid.NewGuid(), UserName = "Admin", Role = "Admin" };

        // Act
        var token = _sut.CreateToken(payload);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        jwt.Claims.Should().Contain(c => c.Value == "Admin");
    }

    [Fact]
    public void CreateToken_WithHotelId_TokenContainsHotelIdClaim()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var payload = new TokenPayloadDto
        {
            UserId = Guid.NewGuid(),
            UserName = "HotelAdmin",
            Role = "Admin",
            HotelId = hotelId
        };

        // Act
        var token = _sut.CreateToken(payload);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        jwt.Claims.Should().Contain(c => c.Type == "HotelId" && c.Value == hotelId.ToString());
    }

    [Fact]
    public void CreateToken_WithoutHotelId_NoHotelIdClaim()
    {
        // Arrange
        var payload = new TokenPayloadDto
        {
            UserId = Guid.NewGuid(),
            UserName = "Guest",
            Role = "Guest",
            HotelId = null
        };

        // Act
        var token = _sut.CreateToken(payload);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        jwt.Claims.Should().NotContain(c => c.Type == "HotelId");
    }

    [Fact]
    public void CreateToken_TokenExpiresInFuture()
    {
        // Arrange
        var payload = new TokenPayloadDto { UserId = Guid.NewGuid(), UserName = "U", Role = "Guest" };

        // Act
        var token = _sut.CreateToken(payload);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void CreateToken_MissingJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var act = () => new TokenService(config);
        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT Key*");
    }
}
