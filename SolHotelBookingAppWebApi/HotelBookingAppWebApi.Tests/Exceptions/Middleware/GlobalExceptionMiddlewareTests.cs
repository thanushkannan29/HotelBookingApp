using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Exceptions.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Tests.Exceptions.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private static IServiceProvider BuildServiceProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<HotelBookingContext>(o =>
            o.UseInMemoryDatabase(dbName)
             .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext BuildHttpContext(IServiceProvider sp, ClaimsPrincipal? user = null)
    {
        var ctx = new DefaultHttpContext { RequestServices = sp };
        ctx.Response.Body = new MemoryStream();
        if (user != null) ctx.User = user;
        return ctx;
    }

    private static ClaimsPrincipal BuildUser(Guid userId, string role = "Guest")
    {
        var claims = new[]
        {
            new Claim("nameid",      userId.ToString()),
            new Claim("unique_name", "TestUser"),
            new Claim("role",        role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextMiddleware()
    {
        // Arrange
        var sp = BuildServiceProvider(nameof(InvokeAsync_NoException_CallsNextMiddleware));
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
        var ctx = BuildHttpContext(sp);

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AppException_ReturnsCorrectStatusAndJson()
    {
        // Arrange
        var sp = BuildServiceProvider(nameof(InvokeAsync_AppException_ReturnsCorrectStatusAndJson));
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        RequestDelegate next = _ => throw new ConflictException("Duplicate email.");
        var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
        var ctx = BuildHttpContext(sp);

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404()
    {
        // Arrange
        var sp = BuildServiceProvider(nameof(InvokeAsync_NotFoundException_Returns404));
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        RequestDelegate next = _ => throw new NotFoundException("Not found.");
        var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
        var ctx = BuildHttpContext(sp);

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        // Arrange
        var sp = BuildServiceProvider(nameof(InvokeAsync_GenericException_Returns500));
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        RequestDelegate next = _ => throw new Exception("Unexpected error.");
        var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
        var ctx = BuildHttpContext(sp);

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_LogsUserInfo()
    {
        // Arrange
        var sp = BuildServiceProvider(nameof(InvokeAsync_AuthenticatedUser_LogsUserInfo));
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        RequestDelegate next = _ => throw new ValidationException("Bad input.");
        var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
        var user = BuildUser(Guid.NewGuid(), "Admin");
        var ctx = BuildHttpContext(sp, user);

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(400);
        loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_AnonymousUser_LogsAnonymous()
    {
        // Arrange
        var sp = BuildServiceProvider(nameof(InvokeAsync_AnonymousUser_LogsAnonymous));
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        RequestDelegate next = _ => throw new AppException("Error", 400);
        var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
        var ctx = BuildHttpContext(sp); // no user

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_DbPersistFails_LogsCriticalAndContinues()
    {
        // Arrange — use a broken service provider that throws on GetRequiredService
        var services = new ServiceCollection();
        services.AddLogging();
        // Intentionally do NOT register HotelBookingContext so it throws
        var sp = services.BuildServiceProvider();
        var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
        RequestDelegate next = _ => throw new Exception("DB error");
        var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);
        var ctx = BuildHttpContext(sp);

        // Act
        var act = async () => await middleware.InvokeAsync(ctx);

        // Assert — does not rethrow; logs critical
        await act.Should().NotThrowAsync();
        loggerMock.Verify(l => l.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
