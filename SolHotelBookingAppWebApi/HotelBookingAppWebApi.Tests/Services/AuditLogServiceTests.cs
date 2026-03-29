using FluentAssertions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.AuditLog;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class AuditLogServiceTests
{
    private readonly Mock<IRepository<Guid, AuditLog>> _auditRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _sut = new AuditLogService(_auditRepo.Object, _userRepo.Object, _unitOfWork.Object);
    }

    private static AuditLog MakeLog(Guid? userId = null, string action = "HotelUpdated", Guid? entityId = null) => new()
    {
        AuditLogId = Guid.NewGuid(),
        UserId = userId,
        Action = action,
        EntityName = "Hotel",
        EntityId = entityId ?? Guid.NewGuid(),
        Changes = "{}",
        CreatedAt = DateTime.UtcNow
    };

    // ── LogAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task LogAsync_ValidInput_CreatesAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog al) => al);

        // Act
        await _sut.LogAsync(userId, "HotelUpdated", "Hotel", entityId, "{\"name\":\"Grand\"}");

        // Assert
        _auditRepo.Verify(r => r.AddAsync(It.Is<AuditLog>(al =>
            al.UserId == userId &&
            al.Action == "HotelUpdated" &&
            al.EntityName == "Hotel" &&
            al.EntityId == entityId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LogAsync_NullUserId_CreatesSystemLog()
    {
        // Arrange
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog al) => al);

        // Act
        await _sut.LogAsync(null, "HotelBlocked", "Hotel", Guid.NewGuid(), "blocked by superadmin");

        // Assert
        _auditRepo.Verify(r => r.AddAsync(It.Is<AuditLog>(al => al.UserId == null)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── GetAdminAuditLogsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminAuditLogsAsync_ReturnsAdminLogs()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var logs = new List<AuditLog>
        {
            MakeLog(adminId, "RoomAdded"),
            MakeLog(adminId, "HotelUpdated", hotelId)
        }.AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAdminAuditLogsAsync(adminId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAdminAuditLogsAsync_WithSearchFilter_FiltersCorrectly()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = null, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var logs = new List<AuditLog>
        {
            MakeLog(adminId, "RoomAdded"),
            MakeLog(adminId, "HotelUpdated")
        }.AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAdminAuditLogsAsync(adminId, 1, 10, search: "Room");

        // Assert
        result.TotalCount.Should().Be(1);
        result.Logs.First().Action.Should().Be("RoomAdded");
    }

    [Fact]
    public async Task GetAdminAuditLogsAsync_EmptyLogs_ReturnsZeroCount()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { UserId = adminId, HotelId = null, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);

        var logs = new List<AuditLog>().AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAdminAuditLogsAsync(adminId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Logs.Should().BeEmpty();
    }

    // ── GetAllAuditLogsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAuditLogsAsync_NoFilters_ReturnsAll()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            MakeLog(Guid.NewGuid(), "HotelBlocked"),
            MakeLog(Guid.NewGuid(), "RoomAdded"),
            MakeLog(Guid.NewGuid(), "ReviewDeleted")
        }.AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllAuditLogsAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAuditLogsAsync_WithUserIdFilter_FiltersCorrectly()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var logs = new List<AuditLog>
        {
            MakeLog(targetUserId, "HotelUpdated"),
            MakeLog(Guid.NewGuid(), "RoomAdded")
        }.AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllAuditLogsAsync(1, 10, userId: targetUserId);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Logs.First().UserId.Should().Be(targetUserId);
    }

    [Fact]
    public async Task GetAllAuditLogsAsync_WithActionFilter_FiltersCorrectly()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            MakeLog(Guid.NewGuid(), "HotelBlocked"),
            MakeLog(Guid.NewGuid(), "HotelUpdated"),
            MakeLog(Guid.NewGuid(), "RoomAdded")
        }.AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllAuditLogsAsync(1, 10, action: "Hotel");

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAuditLogsAsync_WithDateFromFilter_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = Guid.NewGuid(), Action = "Old", EntityName = "Hotel", Changes = "{}", CreatedAt = now.AddDays(-10) },
            new() { AuditLogId = Guid.NewGuid(), Action = "New", EntityName = "Hotel", Changes = "{}", CreatedAt = now.AddDays(-1) }
        }.AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllAuditLogsAsync(1, 10, dateFrom: now.AddDays(-5));

        // Assert
        result.TotalCount.Should().Be(1);
        result.Logs.First().Action.Should().Be("New");
    }

    [Fact]
    public async Task GetAllAuditLogsAsync_WithHotelIdFilter_FiltersCorrectly()
    {
        // Arrange
        var hotelId = Guid.NewGuid();
        var logs = new List<AuditLog>
        {
            MakeLog(Guid.NewGuid(), "HotelUpdated", hotelId),
            MakeLog(Guid.NewGuid(), "RoomAdded", Guid.NewGuid())
        }.AsQueryable().BuildMock();
        _auditRepo.Setup(r => r.GetQueryable()).Returns(logs);

        // Act
        var result = await _sut.GetAllAuditLogsAsync(1, 10, hotelId: hotelId);

        // Assert
        result.TotalCount.Should().Be(1);
    }
}
