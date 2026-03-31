using FluentAssertions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class AuditLogServiceTests
{
    private readonly Mock<IRepository<Guid, AuditLog>> _auditRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private AuditLogService CreateSut() => new(
        _auditRepoMock.Object, _userRepoMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task LogAsync_ValidParams_AddsEntry()
    {
        // Arrange
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync((AuditLog al) => al);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.LogAsync(Guid.NewGuid(), "Create", "Hotel", Guid.NewGuid(), "{}");

        // Assert
        await act.Should().NotThrowAsync();
        _auditRepoMock.Verify(r => r.AddAsync(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task GetAdminAuditLogsAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(
            new List<User> { new() { UserId = adminId, HotelId = hotelId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow } }
            .AsQueryable().BuildMock());
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = Guid.NewGuid(), UserId = adminId, Action = "Update", EntityName = "Hotel", Changes = "{}", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _auditRepoMock.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAdminAuditLogsAsync(adminId, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAuditLogsAsync_NoFilters_ReturnsAll()
    {
        // Arrange
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = Guid.NewGuid(), Action = "Create", EntityName = "Room", Changes = "{}", CreatedAt = DateTime.UtcNow },
            new() { AuditLogId = Guid.NewGuid(), Action = "Delete", EntityName = "Room", Changes = "{}", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _auditRepoMock.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllAuditLogsAsync(1, 10);

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAuditLogsAsync_WithUserIdFilter_ReturnsFiltered()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<AuditLog>
        {
            new() { AuditLogId = Guid.NewGuid(), UserId = userId, Action = "Create", EntityName = "Room", Changes = "{}", CreatedAt = DateTime.UtcNow },
            new() { AuditLogId = Guid.NewGuid(), UserId = Guid.NewGuid(), Action = "Delete", EntityName = "Room", Changes = "{}", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _auditRepoMock.Setup(r => r.GetQueryable()).Returns(logs);
        var sut = CreateSut();

        // Act
        var result = await sut.GetAllAuditLogsAsync(1, 10, userId: userId);

        // Assert
        result.TotalCount.Should().Be(1);
    }
}
