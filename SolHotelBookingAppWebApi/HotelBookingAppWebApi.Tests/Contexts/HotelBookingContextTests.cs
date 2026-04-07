using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HotelBookingAppWebApi.Tests.Contexts;

public class HotelBookingContextTests
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
    public void Constructor_ValidOptions_CreatesContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(nameof(Constructor_ValidOptions_CreatesContext))
            .Options;

        // Act
        var ctx = new HotelBookingContext(options);

        // Assert
        ctx.Should().NotBeNull();
        ctx.Dispose();
    }

    [Fact]
    public void DbSets_AllPropertiesAreNotNull()
    {
        // Arrange
        using var ctx = CreateContext(nameof(DbSets_AllPropertiesAreNotNull));

        // Act & Assert
        ctx.Users.Should().NotBeNull();
        ctx.Hotels.Should().NotBeNull();
        ctx.RoomTypes.Should().NotBeNull();
        ctx.Rooms.Should().NotBeNull();
        ctx.Reservations.Should().NotBeNull();
        ctx.ReservationRooms.Should().NotBeNull();
        ctx.Reviews.Should().NotBeNull();
        ctx.Transactions.Should().NotBeNull();
        ctx.Logs.Should().NotBeNull();
        ctx.AuditLogs.Should().NotBeNull();
        ctx.Amenities.Should().NotBeNull();
        ctx.Wallets.Should().NotBeNull();
        ctx.PromoCodes.Should().NotBeNull();
        ctx.AmenityRequests.Should().NotBeNull();
        ctx.SupportRequests.Should().NotBeNull();
    }

    [Fact]
    public void OnModelCreating_UserEmail_HasUniqueIndex()
    {
        // Arrange
        using var ctx = CreateContext(nameof(OnModelCreating_UserEmail_HasUniqueIndex));

        // Act
        var entityType = ctx.Model.FindEntityType(typeof(User));
        var indexes = entityType!.GetIndexes();

        // Assert
        indexes.Should().Contain(i =>
            i.IsUnique &&
            i.Properties.Any(p => p.Name == nameof(User.Email)));
    }

    [Fact]
    public async Task Users_AddAndQuery_ReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(Users_AddAndQuery_ReturnsEntity));
        var user = new User
        {
            UserId = Guid.NewGuid(), Name = "Alice", Email = "alice@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow
        };

        // Act
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        var found = await ctx.Users.FindAsync(user.UserId);

        // Assert
        found.Should().NotBeNull();
        found!.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task Hotels_AddAndQuery_ReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(Hotels_AddAndQuery_ReturnsEntity));
        var hotel = new Hotel
        {
            HotelId = Guid.NewGuid(), Name = "Grand Hotel", Address = "123 Main St",
            City = "Mumbai", State = "MH", ContactNumber = "9999999999",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        ctx.Hotels.Add(hotel);
        await ctx.SaveChangesAsync();
        var found = await ctx.Hotels.FindAsync(hotel.HotelId);

        // Assert
        found.Should().NotBeNull();
        found!.Name.Should().Be("Grand Hotel");
    }

    [Fact]
    public async Task Reservations_AddAndQuery_ReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(Reservations_AddAndQuery_ReturnsEntity));
        var user = new User
        {
            UserId = Guid.NewGuid(), Name = "Bob", Email = "bob@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow
        };
        var hotel = new Hotel
        {
            HotelId = Guid.NewGuid(), Name = "H1", Address = "A", City = "C",
            ContactNumber = "1234567890", CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(user);
        ctx.Hotels.Add(hotel);
        await ctx.SaveChangesAsync();

        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "RES001",
            UserId = user.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            TotalAmount = 1000, Status = ReservationStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        // Act
        ctx.Reservations.Add(reservation);
        await ctx.SaveChangesAsync();
        var found = await ctx.Reservations.FindAsync(reservation.ReservationId);

        // Assert
        found.Should().NotBeNull();
        found!.ReservationCode.Should().Be("RES001");
    }

    [Fact]
    public async Task Logs_AddAndQuery_ReturnsEntity()
    {
        // Arrange
        using var ctx = CreateContext(nameof(Logs_AddAndQuery_ReturnsEntity));
        var log = new Log
        {
            LogId = Guid.NewGuid(), Message = "Test error", ExceptionType = "Exception",
            StackTrace = "stack", StatusCode = 500, UserName = "Anonymous",
            Role = "Guest", Controller = "Test", Action = "Test",
            HttpMethod = "GET", RequestPath = "/test", CreatedAt = DateTime.UtcNow
        };

        // Act
        ctx.Logs.Add(log);
        await ctx.SaveChangesAsync();
        var found = await ctx.Logs.FindAsync(log.LogId);

        // Assert
        found.Should().NotBeNull();
        found!.Message.Should().Be("Test error");
    }
}
