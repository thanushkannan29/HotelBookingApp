using FluentAssertions;
using HotelBookingAppWebApi.Controllers;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HotelBookingAppWebApi.Tests.Controllers;

public class AuthenticationControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly AuthenticationController _sut;

    public AuthenticationControllerTests()
        => _sut = new AuthenticationController(_authServiceMock.Object);

    [Fact]
    public async Task RegisterGuest_ValidDto_ReturnsOkWithToken()
    {
        // Arrange
        var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };
        _authServiceMock.Setup(s => s.RegisterGuestAsync(dto))
            .ReturnsAsync(new AuthResponseDto { Token = "jwt" });

        // Act
        var result = await _sut.RegisterGuest(dto);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterGuest_ServiceThrows_PropagatesException()
    {
        // Arrange
        _authServiceMock.Setup(s => s.RegisterGuestAsync(It.IsAny<RegisterUserDto>()))
            .ThrowsAsync(new ConflictException("Email already registered."));

        // Act
        var act = async () => await _sut.RegisterGuest(new RegisterUserDto());

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("Email already registered.");
    }

    [Fact]
    public async Task RegisterHotelAdmin_ValidDto_ReturnsOkWithToken()
    {
        // Arrange
        var dto = new RegisterHotelAdminDto
        {
            Name = "Admin", Email = "admin@hotel.com", Password = "pass123",
            HotelName = "Grand Hotel", Address = "123 Main", City = "Mumbai", ContactNumber = "9999999999"
        };
        _authServiceMock.Setup(s => s.RegisterHotelAdminAsync(dto))
            .ReturnsAsync(new AuthResponseDto { Token = "admin-jwt" });

        // Act
        var result = await _sut.RegisterHotelAdmin(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RegisterHotelAdmin_ServiceThrows_PropagatesException()
    {
        // Arrange
        _authServiceMock.Setup(s => s.RegisterHotelAdminAsync(It.IsAny<RegisterHotelAdminDto>()))
            .ThrowsAsync(new ConflictException("Email already registered."));

        // Act
        var act = async () => await _sut.RegisterHotelAdmin(new RegisterHotelAdminDto());

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var dto = new LoginDto { Email = "alice@test.com", Password = "pass123" };
        _authServiceMock.Setup(s => s.LoginAsync(dto))
            .ReturnsAsync(new AuthResponseDto { Token = "jwt" });

        // Act
        var result = await _sut.Login(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_ServiceThrows_PropagatesException()
    {
        // Arrange
        _authServiceMock.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new UnAuthorizedException("Invalid credentials."));

        // Act
        var act = async () => await _sut.Login(new LoginDto());

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }
}
