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
    private readonly Mock<IRepository<Guid, Log>> _logRepoMock = new();

    private LogService CreateSut() => new(_logRepoMock.Object);

    private static Log MakeLog(Guid? userId = null) => new()
    {
        LogId = Guid.NewGuid(), Message = "Error", ExceptionType = "Exception",
        StackTrace = "stack", StatusCode = 500, UserName = "User",
        Role = "Guest", Controller = "Test", Action = "Test",
        HttpMethod = "GET", RequestPath = "/test", CreatedAt = DateTime.UtcNow,
        UserId = userId
    };

    [Fact]
    public async Task GetAllLogsAsync_ValidPagination_ReturnsPaged()
    {
        // Arrange
        var logs = new List<Log> { MakeLog(), MakeLog() }.AsQueryable().BuildMock();
        _logRepoMock.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllLogsAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllLogsAsync_WithSearch_FiltersResults()
    {
        // Arrange
        var log1 = MakeLog(); log1.Message = "NullRef error";
        var log2 = MakeLog(); log2.Message = "Timeout";
        var logs = new List<Log> { log1, log2 }.AsQueryable().BuildMock();
        _logRepoMock.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllLogsAsync(1, 10, "NullRef");

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllLogsAsync_InvalidPage_ThrowsAppException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetAllLogsAsync(0, 10);

        // Assert
        await act.Should().ThrowAsync<AppException>();
    }

    [Fact]
    public async Task GetUserLogsAsync_ValidUser_ReturnsUserLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<Log> { MakeLog(userId), MakeLog(Guid.NewGuid()) }.AsQueryable().BuildMock();
        _logRepoMock.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = CreateSut();

        // Act
        var result = await sut.GetUserLogsAsync(userId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUserLogsAsync_InvalidPageSize_ThrowsAppException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetUserLogsAsync(Guid.NewGuid(), 1, 0);

        // Assert
        await act.Should().ThrowAsync<AppException>();
    }
}
