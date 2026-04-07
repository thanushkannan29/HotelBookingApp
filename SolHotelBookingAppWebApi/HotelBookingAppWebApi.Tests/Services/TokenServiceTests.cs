using FluentAssertions;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using HotelBookingAppWebApi.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace HotelBookingAppWebApi.Tests.Services;

public class TokenServiceTests
{
    private static IConfiguration BuildConfig(string? jwtKey = "super-secret-key-for-testing-1234567890")
    {
        var dict = new Dictionary<string, string?>();
        if (jwtKey != null) dict["Keys:Jwt"] = jwtKey;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static TokenPayloadDto BuildPayload(Guid? hotelId = null) => new()
    {
        UserId = Guid.NewGuid(),
        UserName = "TestUser",
        Role = "Guest",
        HotelId = hotelId
    };

    [Fact]
    public void CreateToken_ValidPayload_ReturnsJwtString()
    {
        // Arrange
        var sut = new TokenService(BuildConfig());
        var payload = BuildPayload();

        // Act
        var result = sut.CreateToken(payload);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateToken_ValidPayload_ContainsUserIdClaim()
    {
        // Arrange
        var sut = new TokenService(BuildConfig());
        var payload = BuildPayload();

        // Act
        var result = sut.CreateToken(payload);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result);
        // JWT short claim type for NameIdentifier is "nameid"
        token.Claims.Should().Contain(c =>
            c.Type == "nameid" &&
            c.Value == payload.UserId.ToString());
    }

    [Fact]
    public void CreateToken_ValidPayload_ContainsUserNameClaim()
    {
        // Arrange
        var sut = new TokenService(BuildConfig());
        var payload = BuildPayload();

        // Act
        var result = sut.CreateToken(payload);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result);
        // JWT short claim type for Name is "unique_name"
        token.Claims.Should().Contain(c =>
            c.Type == "unique_name" &&
            c.Value == "TestUser");
    }

    [Fact]
    public void CreateToken_ValidPayload_ContainsRoleClaim()
    {
        // Arrange
        var sut = new TokenService(BuildConfig());
        var payload = BuildPayload();

        // Act
        var result = sut.CreateToken(payload);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result);
        // JWT short claim type for Role is "role"
        token.Claims.Should().Contain(c =>
            c.Type == "role" &&
            c.Value == "Guest");
    }

    [Fact]
    public void CreateToken_WithHotelId_ContainsHotelIdClaim()
    {
        // Arrange
        var sut = new TokenService(BuildConfig());
        var hotelId = Guid.NewGuid();
        var payload = BuildPayload(hotelId);

        // Act
        var result = sut.CreateToken(payload);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result);
        token.Claims.Should().Contain(c => c.Type == "HotelId" && c.Value == hotelId.ToString());
    }

    [Fact]
    public void CreateToken_WithoutHotelId_DoesNotContainHotelIdClaim()
    {
        // Arrange
        var sut = new TokenService(BuildConfig());
        var payload = BuildPayload(null);

        // Act
        var result = sut.CreateToken(payload);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result);
        token.Claims.Should().NotContain(c => c.Type == "HotelId");
    }

    [Fact]
    public void Constructor_MissingJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = BuildConfig(null);

        // Act
        var act = () => new TokenService(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT Key*");
    }
}
