using FluentAssertions;
using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Repository;
using HotelBookingAppWebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

/// <summary>
/// Uses real Repository + InMemory EF so the actual ReservationService code paths execute.
/// </summary>
public class ReservationServiceTests
{
    private readonly Mock<IWalletService> _walletMock = new();
    private readonly Mock<IPromoCodeService> _promoMock = new();
    private readonly Mock<ISuperAdminRevenueService> _revenueMock = new();

    private static HotelBookingContext CreateContext(string dbName)
    {
        var opts = new DbContextOptionsBuilder<HotelBookingContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HotelBookingContext(opts);
    }

    private ReservationService CreateSut(HotelBookingContext ctx) => new(
        new Repository<Guid, Reservation>(ctx),
        new Repository<Guid, Room>(ctx),
        new Repository<Guid, RoomType>(ctx),
        new Repository<Guid, RoomTypeInventory>(ctx),
        new Repository<Guid, RoomTypeRate>(ctx),
        new Repository<Guid, ReservationRoom>(ctx),
        new Repository<Guid, Hotel>(ctx),
        new Repository<Guid, User>(ctx),
        _walletMock.Object,
        _promoMock.Object,
        _revenueMock.Object,
        new UnitOfWork(ctx));

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private static async Task<(Hotel hotel, RoomType roomType, Room room, User guest)>
        SeedBasicDataAsync(HotelBookingContext ctx, string dbName)
    {
        var hotel = new Hotel
        {
            HotelId = Guid.NewGuid(), Name = "Test Hotel", Address = "123 Main",
            City = "Mumbai", State = "MH", ContactNumber = "9999999999",
            IsActive = true, GstPercent = 0, CreatedAt = DateTime.UtcNow
        };
        var roomType = new RoomType
        {
            RoomTypeId = Guid.NewGuid(), HotelId = hotel.HotelId,
            Name = "Deluxe", MaxOccupancy = 2, IsActive = true
        };
        var room = new Room
        {
            RoomId = Guid.NewGuid(), HotelId = hotel.HotelId,
            RoomTypeId = roomType.RoomTypeId, RoomNumber = "101", Floor = 1, IsActive = true
        };
        var guest = new User
        {
            UserId = Guid.NewGuid(), Name = "Alice", Email = $"alice_{dbName}@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow
        };
        ctx.Hotels.Add(hotel);
        ctx.RoomTypes.Add(roomType);
        ctx.Rooms.Add(room);
        ctx.Users.Add(guest);
        await ctx.SaveChangesAsync();
        return (hotel, roomType, room, guest);
    }

    private static async Task SeedInventoryAndRateAsync(
        HotelBookingContext ctx, Guid roomTypeId,
        DateOnly checkIn, DateOnly checkOut, decimal rate = 1000m)
    {
        var days = checkOut.DayNumber - checkIn.DayNumber;
        for (int i = 0; i < days; i++)
        {
            ctx.RoomTypeInventories.Add(new RoomTypeInventory
            {
                RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId,
                Date = checkIn.AddDays(i), TotalInventory = 5, ReservedInventory = 0
            });
        }
        ctx.RoomTypeRates.Add(new RoomTypeRate
        {
            RoomTypeRateId = Guid.NewGuid(), RoomTypeId = roomTypeId,
            StartDate = checkIn, EndDate = checkOut.AddDays(30), Rate = rate
        });
        await ctx.SaveChangesAsync();
    }

    // ── GetMyReservationsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyReservationsAsync_NoReservations_ReturnsEmpty()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetMyReservationsAsync_NoReservations_ReturnsEmpty));
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetMyReservationsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyReservationsAsync_WithReservations_ReturnsAll()
    {
        // Arrange
        var dbName = nameof(GetMyReservationsAsync_WithReservations_ReturnsAll);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, guest) = await SeedBasicDataAsync(ctx, dbName);
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            TotalAmount = 2000, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetMyReservationsAsync(guest.UserId);

        // Assert
        result.Should().HaveCount(1);
    }

    // ── GetMyReservationsPagedAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetMyReservationsPagedAsync_WithStatusFilter_ReturnsFiltered()
    {
        // Arrange
        var dbName = nameof(GetMyReservationsPagedAsync_WithStatusFilter_ReturnsFiltered);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, guest) = await SeedBasicDataAsync(ctx, dbName);
        ctx.Reservations.AddRange(
            new Reservation { ReservationId = Guid.NewGuid(), ReservationCode = "R001", UserId = guest.UserId, HotelId = hotel.HotelId, CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)), TotalAmount = 2000, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow },
            new Reservation { ReservationId = Guid.NewGuid(), ReservationCode = "R002", UserId = guest.UserId, HotelId = hotel.HotelId, CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(12)), TotalAmount = 2000, Status = ReservationStatus.Confirmed, CreatedDate = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetMyReservationsPagedAsync(guest.UserId, 1, 10, "Pending");

        // Assert
        result.TotalCount.Should().Be(1);
        result.Reservations.First().ReservationCode.Should().Be("R001");
    }

    // ── GetReservationByCodeAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetReservationByCodeAsync_ValidCode_ReturnsDetails()
    {
        // Arrange
        var dbName = nameof(GetReservationByCodeAsync_ValidCode_ReturnsDetails);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, guest) = await SeedBasicDataAsync(ctx, dbName);
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            TotalAmount = 2000, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetReservationByCodeAsync(guest.UserId, "R001");

        // Assert
        result.ReservationCode.Should().Be("R001");
    }

    [Fact]
    public async Task GetReservationByCodeAsync_InvalidCode_ThrowsNotFoundException()
    {
        // Arrange
        var dbName = nameof(GetReservationByCodeAsync_InvalidCode_ThrowsNotFoundException);
        using var ctx = CreateContext(dbName);
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.GetReservationByCodeAsync(Guid.NewGuid(), "INVALID");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── ConfirmReservationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ConfirmReservationAsync_PendingReservation_Confirms()
    {
        // Arrange
        var dbName = nameof(ConfirmReservationAsync_PendingReservation_Confirms);
        using var ctx = CreateContext(dbName);
        var (hotel, _, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            TotalAmount = 2000, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow
        };
        ctx.Reservations.Add(reservation);
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.ConfirmReservationAsync("R001");

        // Assert
        result.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task ConfirmReservationAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dbName = nameof(ConfirmReservationAsync_NotFound_ThrowsNotFoundException);
        using var ctx = CreateContext(dbName);
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.ConfirmReservationAsync("INVALID");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ConfirmReservationAsync_AlreadyConfirmed_ThrowsValidationException()
    {
        // Arrange
        var dbName = nameof(ConfirmReservationAsync_AlreadyConfirmed_ThrowsValidationException);
        using var ctx = CreateContext(dbName);
        var (hotel, _, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            TotalAmount = 2000, Status = ReservationStatus.Confirmed, CreatedDate = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.ConfirmReservationAsync("R001");

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    // ── CompleteReservationAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CompleteReservationAsync_ConfirmedReservation_Completes()
    {
        // Arrange
        var dbName = nameof(CompleteReservationAsync_ConfirmedReservation_Completes);
        using var ctx = CreateContext(dbName);
        var (hotel, _, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),   // must be today
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            TotalAmount = 2000, Status = ReservationStatus.Confirmed, CreatedDate = DateTime.UtcNow
        };
        ctx.Reservations.Add(reservation);
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.CompleteReservationAsync("R001");

        // Assert
        result.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Completed);
        reservation.IsCheckedIn.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteReservationAsync_NotConfirmed_ThrowsValidationException()
    {
        // Arrange
        var dbName = nameof(CompleteReservationAsync_NotConfirmed_ThrowsValidationException);
        using var ctx = CreateContext(dbName);
        var (hotel, _, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            TotalAmount = 2000, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.CompleteReservationAsync("R001");

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CompleteReservationAsync_NotOnCheckinDate_ThrowsValidationException()
    {
        // Arrange
        var dbName = nameof(CompleteReservationAsync_NotOnCheckinDate_ThrowsValidationException);
        using var ctx = CreateContext(dbName);
        var (hotel, _, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)), // future date — not today
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            TotalAmount = 2000, Status = ReservationStatus.Confirmed, CreatedDate = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.CompleteReservationAsync("R001");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*check-in date*");
    }

    // ── GetAdminReservationsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAdminReservationsAsync_ValidAdmin_ReturnsPaged()
    {
        // Arrange
        var dbName = nameof(GetAdminReservationsAsync_ValidAdmin_ReturnsPaged);
        using var ctx = CreateContext(dbName);
        var (hotel, _, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        var admin = new User
        {
            UserId = Guid.NewGuid(), Name = "Admin", Email = $"admin_{dbName}@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Admin, HotelId = hotel.HotelId, CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(admin);
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            TotalAmount = 2000, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetAdminReservationsAsync(admin.UserId, "All", null, 1, 10);

        // Assert
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminReservationsAsync_AdminHasNoHotel_ThrowsUnAuthorizedException()
    {
        // Arrange
        var dbName = nameof(GetAdminReservationsAsync_AdminHasNoHotel_ThrowsUnAuthorizedException);
        using var ctx = CreateContext(dbName);
        var admin = new User
        {
            UserId = Guid.NewGuid(), Name = "Admin", Email = $"admin_{dbName}@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Admin, HotelId = null, CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.GetAdminReservationsAsync(admin.UserId, "All", null, 1, 10);

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    // ── GetAvailableRoomsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableRoomsAsync_NoBookings_ReturnsAllRooms()
    {
        // Arrange
        var dbName = nameof(GetAvailableRoomsAsync_NoBookings_ReturnsAllRooms);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, _) = await SeedBasicDataAsync(ctx, dbName);
        var sut = CreateSut(ctx);
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(5));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        // Act
        var result = await sut.GetAvailableRoomsAsync(hotel.HotelId, roomType.RoomTypeId, checkIn, checkOut);

        // Assert
        result.Should().HaveCount(1);
    }

    // ── CancelReservationAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CancelReservationAsync_PendingNoPayment_CancelsWithoutRefund()
    {
        // Arrange
        var dbName = nameof(CancelReservationAsync_PendingNoPayment_CancelsWithoutRefund);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, guest) = await SeedBasicDataAsync(ctx, dbName);
        var reservationId = Guid.NewGuid();
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = reservationId, ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(12)),
            TotalAmount = 2000, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>
            {
                new() { ReservationRoomId = Guid.NewGuid(), ReservationId = reservationId, RoomTypeId = roomType.RoomTypeId, RoomId = room.RoomId, PricePerNight = 1000 }
            }
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.CancelReservationAsync(guest.UserId, "R001", "Changed plans");

        // Assert
        result.Should().BeTrue();
        _walletMock.Verify(w => w.CreditAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CancelReservationAsync_AlreadyCancelled_ThrowsReservationFailedException()
    {
        // Arrange
        var dbName = nameof(CancelReservationAsync_AlreadyCancelled_ThrowsReservationFailedException);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, guest) = await SeedBasicDataAsync(ctx, dbName);
        var reservationId = Guid.NewGuid();
        ctx.Reservations.Add(new Reservation
        {
            ReservationId = reservationId, ReservationCode = "R001",
            UserId = guest.UserId, HotelId = hotel.HotelId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(12)),
            TotalAmount = 2000, Status = ReservationStatus.Cancelled, CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>
            {
                new() { ReservationRoomId = Guid.NewGuid(), ReservationId = reservationId, RoomTypeId = roomType.RoomTypeId, RoomId = room.RoomId, PricePerNight = 1000 }
            }
        });
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.CancelReservationAsync(guest.UserId, "R001", "Changed plans");

        // Assert
        await act.Should().ThrowAsync<ReservationFailedException>();
    }

    [Fact]
    public async Task CancelReservationAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dbName = nameof(CancelReservationAsync_NotFound_ThrowsNotFoundException);
        using var ctx = CreateContext(dbName);
        var sut = CreateSut(ctx);

        // Act
        var act = async () => await sut.CancelReservationAsync(Guid.NewGuid(), "INVALID", "reason");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── CreateReservationAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateReservationAsync_ValidData_CreatesReservation()
    {
        // Arrange
        var dbName = nameof(CreateReservationAsync_ValidData_CreatesReservation);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, guest) = await SeedBasicDataAsync(ctx, dbName);
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(4));
        await SeedInventoryAndRateAsync(ctx, roomType.RoomTypeId, checkIn, checkOut);
        var sut = CreateSut(ctx);
        var dto = new CreateReservationDto
        {
            HotelId = hotel.HotelId, RoomTypeId = roomType.RoomTypeId,
            CheckInDate = checkIn, CheckOutDate = checkOut, NumberOfRooms = 1
        };

        // Act
        var result = await sut.CreateReservationAsync(guest.UserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.ReservationCode.Should().NotBeNullOrEmpty();
        result.TotalAmount.Should().Be(2000m); // 2 days * 1000
    }

    [Fact]
    public async Task CreateReservationAsync_CheckInInPast_ThrowsValidationException()
    {
        // Arrange
        var dbName = nameof(CreateReservationAsync_CheckInInPast_ThrowsValidationException);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        var sut = CreateSut(ctx);
        var dto = new CreateReservationDto
        {
            HotelId = hotel.HotelId, RoomTypeId = roomType.RoomTypeId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            NumberOfRooms = 1
        };

        // Act
        var act = async () => await sut.CreateReservationAsync(guest.UserId, dto);

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*tomorrow*");
    }

    [Fact]
    public async Task CreateReservationAsync_CheckOutBeforeCheckIn_ThrowsValidationException()
    {
        // Arrange
        var dbName = nameof(CreateReservationAsync_CheckOutBeforeCheckIn_ThrowsValidationException);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, _, guest) = await SeedBasicDataAsync(ctx, dbName);
        var sut = CreateSut(ctx);
        var dto = new CreateReservationDto
        {
            HotelId = hotel.HotelId, RoomTypeId = roomType.RoomTypeId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            NumberOfRooms = 1
        };

        // Act
        var act = async () => await sut.CreateReservationAsync(guest.UserId, dto);

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage("*Check-out*");
    }

    [Fact]
    public async Task CreateReservationAsync_HotelNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var dbName = nameof(CreateReservationAsync_HotelNotFound_ThrowsNotFoundException);
        using var ctx = CreateContext(dbName);
        var sut = CreateSut(ctx);
        var dto = new CreateReservationDto
        {
            HotelId = Guid.NewGuid(), RoomTypeId = Guid.NewGuid(),
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            NumberOfRooms = 1
        };

        // Act
        var act = async () => await sut.CreateReservationAsync(Guid.NewGuid(), dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateReservationAsync_NoInventory_ThrowsInsufficientInventoryException()
    {
        // Arrange
        var dbName = nameof(CreateReservationAsync_NoInventory_ThrowsInsufficientInventoryException);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, guest) = await SeedBasicDataAsync(ctx, dbName);
        // No inventory seeded
        var sut = CreateSut(ctx);
        var dto = new CreateReservationDto
        {
            HotelId = hotel.HotelId, RoomTypeId = roomType.RoomTypeId,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            NumberOfRooms = 1
        };

        // Act
        var act = async () => await sut.CreateReservationAsync(guest.UserId, dto);

        // Assert
        await act.Should().ThrowAsync<InsufficientInventoryException>();
    }

    // ── GetRoomOccupancyAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRoomOccupancyAsync_ValidAdmin_ReturnsOccupancy()
    {
        // Arrange
        var dbName = nameof(GetRoomOccupancyAsync_ValidAdmin_ReturnsOccupancy);
        using var ctx = CreateContext(dbName);
        var (hotel, roomType, room, _) = await SeedBasicDataAsync(ctx, dbName);
        var admin = new User
        {
            UserId = Guid.NewGuid(), Name = "Admin", Email = $"admin_{dbName}@test.com",
            Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 2 },
            Role = UserRole.Admin, HotelId = hotel.HotelId, CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(admin);
        await ctx.SaveChangesAsync();
        var sut = CreateSut(ctx);

        // Act
        var result = await sut.GetRoomOccupancyAsync(admin.UserId, DateOnly.FromDateTime(DateTime.Today));

        // Assert
        result.Should().HaveCount(1);
    }
}
