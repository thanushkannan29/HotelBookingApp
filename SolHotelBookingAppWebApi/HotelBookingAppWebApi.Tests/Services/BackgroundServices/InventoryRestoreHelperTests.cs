using FluentAssertions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services.BackgroundServices;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services.BackgroundServices;

public class InventoryRestoreHelperTests
{
    [Fact]
    public async Task BuildInventoryLookupAsync_ValidReservations_ReturnsLookup()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R1",
            UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(),
            CheckInDate = date, CheckOutDate = date.AddDays(2),
            TotalAmount = 100, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>
            {
                new() { ReservationRoomId = Guid.NewGuid(), RoomTypeId = roomTypeId,
                    RoomId = Guid.NewGuid(), PricePerNight = 50 }
            }
        };

        var inventory = new RoomTypeInventory
        {
            RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId,
            Date = date, TotalInventory = 10, ReservedInventory = 2
        };

        var inventoryRepoMock = new Mock<IRepository<Guid, RoomTypeInventory>>();
        var queryable = new List<RoomTypeInventory> { inventory }.AsQueryable().BuildMock();
        inventoryRepoMock.Setup(r => r.GetQueryable()).Returns(queryable);

        // Act
        var result = await InventoryRestoreHelper.BuildInventoryLookupAsync(
            new List<Reservation> { reservation }, inventoryRepoMock.Object, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey((roomTypeId, date));
    }

    [Fact]
    public void RestoreInventory_ValidReservation_DecrementsReservedCount()
    {
        // Arrange
        var roomTypeId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var inventory = new RoomTypeInventory
        {
            RoomTypeInventoryId = Guid.NewGuid(), RoomTypeId = roomTypeId,
            Date = date, TotalInventory = 10, ReservedInventory = 3
        };
        var lookup = new Dictionary<(Guid, DateOnly), RoomTypeInventory>
        {
            { (roomTypeId, date), inventory }
        };
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R1",
            UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(),
            CheckInDate = date, CheckOutDate = date.AddDays(1),
            TotalAmount = 100, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>
            {
                new() { ReservationRoomId = Guid.NewGuid(), RoomTypeId = roomTypeId,
                    RoomId = Guid.NewGuid(), PricePerNight = 50 }
            }
        };

        // Act
        InventoryRestoreHelper.RestoreInventory(reservation, lookup);

        // Assert
        inventory.ReservedInventory.Should().Be(2);
    }

    [Fact]
    public void RestoreInventory_NoReservationRooms_DoesNothing()
    {
        // Arrange
        var lookup = new Dictionary<(Guid, DateOnly), RoomTypeInventory>();
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(), ReservationCode = "R1",
            UserId = Guid.NewGuid(), HotelId = Guid.NewGuid(),
            CheckInDate = DateOnly.FromDateTime(DateTime.Today),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TotalAmount = 100, Status = ReservationStatus.Pending, CreatedDate = DateTime.UtcNow,
            ReservationRooms = new List<ReservationRoom>()
        };

        // Act
        var act = () => InventoryRestoreHelper.RestoreInventory(reservation, lookup);

        // Assert
        act.Should().NotThrow();
    }
}
