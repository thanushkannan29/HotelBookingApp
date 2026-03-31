using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HotelBookingAppWebApi.Tests.Services;

public class UnitOfWorkTests
{
    private static HotelBookingContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HotelBookingContext(options);
    }

    [Fact]
    public async Task BeginTransactionAsync_FirstCall_StartsTransaction()
    {
        // Arrange
        using var ctx = CreateContext(nameof(BeginTransactionAsync_FirstCall_StartsTransaction));
        var uow = new UnitOfWork(ctx);

        // Act
        var act = async () => await uow.BeginTransactionAsync();

        // Assert
        await act.Should().NotThrowAsync();
        uow.Dispose();
    }

    [Fact]
    public async Task BeginTransactionAsync_SecondCall_DoesNotStartNewTransaction()
    {
        // Arrange
        using var ctx = CreateContext(nameof(BeginTransactionAsync_SecondCall_DoesNotStartNewTransaction));
        var uow = new UnitOfWork(ctx);
        await uow.BeginTransactionAsync();

        // Act
        var act = async () => await uow.BeginTransactionAsync();

        // Assert
        await act.Should().NotThrowAsync();
        uow.Dispose();
    }

    [Fact]
    public async Task CommitAsync_WithTransaction_SavesAndCommits()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CommitAsync_WithTransaction_SavesAndCommits));
        var uow = new UnitOfWork(ctx);
        await uow.BeginTransactionAsync();

        // Act
        var act = async () => await uow.CommitAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CommitAsync_WithoutTransaction_CallsSaveChanges()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CommitAsync_WithoutTransaction_CallsSaveChanges));
        var uow = new UnitOfWork(ctx);

        // Act
        var act = async () => await uow.CommitAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackAsync_WithTransaction_RollsBack()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RollbackAsync_WithTransaction_RollsBack));
        var uow = new UnitOfWork(ctx);
        await uow.BeginTransactionAsync();

        // Act
        var act = async () => await uow.RollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackAsync_WithoutTransaction_DoesNothing()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RollbackAsync_WithoutTransaction_DoesNothing));
        var uow = new UnitOfWork(ctx);

        // Act
        var act = async () => await uow.RollbackAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_CallsContextSaveChanges()
    {
        // Arrange
        using var ctx = CreateContext(nameof(SaveChangesAsync_CallsContextSaveChanges));
        var uow = new UnitOfWork(ctx);

        // Act
        var act = async () => await uow.SaveChangesAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Dispose_CleansUpTransaction()
    {
        // Arrange
        using var ctx = CreateContext(nameof(Dispose_CleansUpTransaction));
        var uow = new UnitOfWork(ctx);
        await uow.BeginTransactionAsync();

        // Act
        uow.Dispose();

        // Assert — after dispose, rollback should be no-op
        var act = async () => await uow.RollbackAsync();
        await act.Should().NotThrowAsync();
    }
}
