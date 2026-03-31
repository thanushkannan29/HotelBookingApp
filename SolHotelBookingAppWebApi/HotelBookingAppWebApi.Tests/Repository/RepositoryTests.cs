using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HotelBookingAppWebApi.Tests.Repository;

public class RepositoryTests
{
    private static HotelBookingContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HotelBookingContext(options);
    }

    private static User MakeUser() => new()
    {
        UserId = Guid.NewGuid(),
        Name = "Test User",
        Email = $"{Guid.NewGuid()}@test.com",
        Password = new byte[] { 1, 2, 3 },
        PasswordSaltValue = new byte[] { 4, 5, 6 },
        Role = UserRole.Guest,
        CreatedAt = DateTime.UtcNow
    };

    // ── AddAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsAddedEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(AddAsync_ValidEntity_ReturnsAddedEntity));
        var repo = new Repository<Guid, User>(ctx);
        var user = MakeUser();

        // Act
        var result = await repo.AddAsync(user);
        await ctx.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
    }

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        using var ctx = CreateContext(nameof(AddAsync_NullEntity_ThrowsArgumentNullException));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var act = async () => await repo.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ExistingKey_ReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAsync_ExistingKey_ReturnsEntity));
        var repo = new Repository<Guid, User>(ctx);
        var user = MakeUser();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Act
        var result = await repo.GetAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsNull()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAsync_MissingKey_ReturnsNull));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = await repo.GetAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAllAsync_WithEntities_ReturnsAllEntities));
        var repo = new Repository<Guid, User>(ctx);
        ctx.Users.AddRange(MakeUser(), MakeUser());
        await ctx.SaveChangesAsync();

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_EmptyTable_ReturnsEmptyList()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAllAsync_EmptyTable_ReturnsEmptyList));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingKey_RemovesAndReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(DeleteAsync_ExistingKey_RemovesAndReturnsEntity));
        var repo = new Repository<Guid, User>(ctx);
        var user = MakeUser();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Act
        var result = await repo.DeleteAsync(user.UserId);
        await ctx.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        ctx.Users.Find(user.UserId).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_MissingKey_ReturnsNull()
    {
        // Arrange
        using var ctx = CreateContext(nameof(DeleteAsync_MissingKey_ReturnsNull));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = await repo.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingKey_UpdatesAndReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(UpdateAsync_ExistingKey_UpdatesAndReturnsEntity));
        var repo = new Repository<Guid, User>(ctx);
        var user = MakeUser();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        var updated = new User
        {
            UserId = user.UserId, Name = "Updated", Email = user.Email,
            Password = user.Password, PasswordSaltValue = user.PasswordSaltValue,
            Role = user.Role, CreatedAt = user.CreatedAt
        };

        // Act
        var result = await repo.UpdateAsync(user.UserId, updated);
        await ctx.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_MissingKey_ReturnsNull()
    {
        // Arrange
        using var ctx = CreateContext(nameof(UpdateAsync_MissingKey_ReturnsNull));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = await repo.UpdateAsync(Guid.NewGuid(), MakeUser());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ReturnsNull()
    {
        // Arrange
        using var ctx = CreateContext(nameof(UpdateAsync_NullEntity_ReturnsNull));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = await repo.UpdateAsync(Guid.NewGuid(), null!);

        // Assert
        result.Should().BeNull();
    }

    // ── FirstOrDefaultAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task FirstOrDefaultAsync_MatchingPredicate_ReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(FirstOrDefaultAsync_MatchingPredicate_ReturnsEntity));
        var repo = new Repository<Guid, User>(ctx);
        var user = MakeUser();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Act
        var result = await repo.FirstOrDefaultAsync(u => u.UserId == user.UserId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsNull()
    {
        // Arrange
        using var ctx = CreateContext(nameof(FirstOrDefaultAsync_NoMatch_ReturnsNull));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = await repo.FirstOrDefaultAsync(u => u.Email == "nobody@test.com");

        // Assert
        result.Should().BeNull();
    }

    // ── GetQueryable ──────────────────────────────────────────────────────────

    [Fact]
    public void GetQueryable_ReturnsNonNullIQueryable()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetQueryable_ReturnsNonNullIQueryable));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = repo.GetQueryable();

        // Assert
        result.Should().NotBeNull();
    }

    // ── GetAllByForeignKeyAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllByForeignKeyAsync_MatchingPredicate_ReturnsCorrectPage()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAllByForeignKeyAsync_MatchingPredicate_ReturnsCorrectPage));
        var repo = new Repository<Guid, User>(ctx);
        var role = UserRole.Guest;
        for (int i = 0; i < 5; i++) ctx.Users.Add(MakeUser());
        await ctx.SaveChangesAsync();

        // Act
        var result = await repo.GetAllByForeignKeyAsync(u => u.Role == role, 3, 1);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllByForeignKeyAsync_NoMatch_ReturnsEmpty()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetAllByForeignKeyAsync_NoMatch_ReturnsEmpty));
        var repo = new Repository<Guid, User>(ctx);

        // Act
        var result = await repo.GetAllByForeignKeyAsync(u => u.Email == "nobody@x.com", 10, 1);

        // Assert
        result.Should().BeEmpty();
    }
}
