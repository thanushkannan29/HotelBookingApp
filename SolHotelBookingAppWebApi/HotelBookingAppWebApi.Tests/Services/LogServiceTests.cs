using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class LogServiceTests
{
    private readonly Mock<IRepository<Guid, Log>> _logRepo = new();
    private readonly LogService _sut;

    public LogServiceTests()
    {
        _sut = new LogService(_logRepo.Object);
    }

    private static Log MakeLog(Guid? userId = null, string path = "/api/hotels", string exType = "NotFoundException") => new()
    {
        LogId = Guid.NewGuid(),
        Message = "Error occurred",
        ExceptionType = exType,
        StackTrace = "at ...",
        StatusCode = 404,
        UserName = "testuser",
        Role = "Guest",
        UserId = userId,
        Controller = "HotelController",
        Action = "Get",
        HttpMethod = "GET",
        RequestPath = path,
        CreatedAt = DateTime.UtcNow
    };

    // ── GetAllLogsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllLogsAsync_NoSearch_ReturnsAllLogs()
    {
        // Arrange
        var logs = new List<Log>
        {
            MakeLog(), MakeLog(), MakeLog()
        }.AsQueryable().BuildMock();
        _logRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllLogsAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Logs.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllLogsAsync_WithSearch_FiltersCorrectly()
    {
        // Arrange
        var logs = new List<Log>
        {
            MakeLog(path: "/api/hotels", exType: "NotFoundException"),
            MakeLog(path: "/api/rooms", exType: "ValidationException")
        }.AsQueryable().BuildMock();
        _logRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllLogsAsync(1, 10, search: "hotels");

        // Assert
        result.TotalCount.Should().Be(1);
        result.Logs.First().RequestPath.Should().Be("/api/hotels");
    }

    [Fact]
    public async Task GetAllLogsAsync_InvalidPage_ThrowsAppException()
    {
        // Arrange & Act & Assert
        await _sut.Invoking(s => s.GetAllLogsAsync(0, 10))
            .Should().ThrowAsync<AppException>().WithMessage("*pagination*");
    }

    [Fact]
    public async Task GetAllLogsAsync_InvalidPageSize_ThrowsAppException()
    {
        // Arrange & Act & Assert
        await _sut.Invoking(s => s.GetAllLogsAsync(1, 0))
            .Should().ThrowAsync<AppException>().WithMessage("*pagination*");
    }

    [Fact]
    public async Task GetAllLogsAsync_EmptyLogs_ReturnsZeroCount()
    {
        // Arrange
        var logs = new List<Log>().AsQueryable().BuildMock();
        _logRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllLogsAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Logs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllLogsAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var logs = Enumerable.Range(1, 25).Select(i => MakeLog(path: $"/api/test/{i}")).ToList().AsQueryable().BuildMock();
        _logRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllLogsAsync(2, 10);

        // Assert
        result.TotalCount.Should().Be(25);
        result.Logs.Should().HaveCount(10);
    }

    // ── GetUserLogsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserLogsAsync_ValidUser_ReturnsUserLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<Log>
        {
            MakeLog(userId),
            MakeLog(userId),
            MakeLog(Guid.NewGuid()) // different user
        }.AsQueryable().BuildMock();
        _logRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetUserLogsAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserLogsAsync_NoLogs_ReturnsEmpty()
    {
        // Arrange
        var logs = new List<Log>().AsQueryable().BuildMock();
        _logRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetUserLogsAsync(Guid.NewGuid(), 1, 10);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Logs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserLogsAsync_InvalidPage_ThrowsAppException()
    {
        // Arrange & Act & Assert
        await _sut.Invoking(s => s.GetUserLogsAsync(Guid.NewGuid(), -1, 10))
            .Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task GetUserLogsAsync_InvalidPageSize_ThrowsAppException()
    {
        // Arrange & Act & Assert
        await _sut.Invoking(s => s.GetUserLogsAsync(Guid.NewGuid(), 1, 0))
            .Should().ThrowAsync<AppException>();
    }
}
