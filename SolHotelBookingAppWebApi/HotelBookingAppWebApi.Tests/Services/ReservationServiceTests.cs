using Xunit;
using Moq;
using MockQueryable.Moq;          // ← ADDED: gives .BuildMock() on any List<T>
using FluentAssertions;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Exceptions;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Tests.Services
{
    /// <summary>
    /// Tests for ReservationService.
    /// Core logic: 12-step booking creation, cancellation, completion, available rooms.
    /// Key rule: inventory must be available on every date in the stay range.
    ///
    /// FIX APPLIED:
    ///   All .AsQueryable() replaced with .AsQueryable().BuildMock()
    ///   so EF Core async methods (ToListAsync, FirstOrDefaultAsync, CountAsync) work.
    ///   Requires NuGet: MockQueryable.Moq version 7.0.0
    /// </summary>
    public class ReservationServiceTests
    {
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IRepository<Guid, Room>> _roomRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomTypeRate>> _rateRepoMock = new();
        private readonly Mock<IRepository<Guid, ReservationRoom>> _reservationRoomRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IRefundRequestService> _refundRequestServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ReservationService _sut;

        public ReservationServiceTests()
        {
            _sut = new ReservationService(
                _reservationRepoMock.Object,
                _roomRepoMock.Object,
                _roomTypeRepoMock.Object,
                _inventoryRepoMock.Object,
                _rateRepoMock.Object,
                _reservationRoomRepoMock.Object,
                _userRepoMock.Object,
                _refundRequestServiceMock.Object,
                _unitOfWorkMock.Object);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
        private static DateOnly Tomorrow => Today.AddDays(1);
        private static DateOnly DayAfter => Today.AddDays(2);

        /// <summary>
        /// KEY FIX: wraps any list into a BuildMock() queryable so EF Core
        /// async LINQ methods (ToListAsync, FirstOrDefaultAsync, CountAsync, AnyAsync)
        /// work inside the service without hitting a real database.
        /// </summary>
        private static IQueryable<T> ToMockQueryable<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        /// <summary>Creates a standard CreateReservationDto for tests.</summary>
        private static CreateReservationDto MakeDto(
            Guid hotelId, Guid roomTypeId,
            DateOnly? checkIn = null, DateOnly? checkOut = null,
            int rooms = 1, List<Guid>? selectedRoomIds = null) =>
            new CreateReservationDto
            {
                HotelId = hotelId,
                RoomTypeId = roomTypeId,
                CheckInDate = checkIn ?? Tomorrow,
                CheckOutDate = checkOut ?? DayAfter,
                NumberOfRooms = rooms,
                SelectedRoomIds = selectedRoomIds
            };

        /// <summary>Builds a valid inventory list for the given date range.</summary>
        private static List<RoomTypeInventory> BuildInventory(
            Guid roomTypeId, DateOnly start, DateOnly end,
            int total = 5, int reserved = 0)
        {
            var list = new List<RoomTypeInventory>();
            for (var d = start; d < end; d = d.AddDays(1))
                list.Add(new RoomTypeInventory
                {
                    RoomTypeInventoryId = Guid.NewGuid(),
                    RoomTypeId = roomTypeId,
                    Date = d,
                    TotalInventory = total,
                    ReservedInventory = reserved
                });
            return list;
        }

        /// <summary>Builds a rate that covers the full date range plus buffer.</summary>
        private static List<RoomTypeRate> BuildRates(
            Guid roomTypeId, DateOnly start, DateOnly end, decimal rate = 1000m) =>
            new List<RoomTypeRate>
            {
                new RoomTypeRate
                {
                    RoomTypeRateId = Guid.NewGuid(),
                    RoomTypeId     = roomTypeId,
                    StartDate      = start.AddDays(-30),
                    EndDate        = end.AddDays(30),
                    Rate           = rate
                }
            };

        // ═══════════════════════════════════════════════════════════════════════
        // CreateReservationAsync — Input Validation Tests
        // These throw before any repo call, so no BuildMock needed here
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateReservationAsync_WithPastCheckIn_ThrowsValidationException()
        {
            // Arrange: check-in is yesterday
            var dto = MakeDto(Guid.NewGuid(), Guid.NewGuid(),
                checkIn: Today.AddDays(-1), checkOut: Today);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
            ex.Message.Should().Contain("past");
        }

        [Fact]
        public async Task CreateReservationAsync_WhenCheckOutBeforeCheckIn_ThrowsValidationException()
        {
            // Arrange: checkout same as checkin — must be AFTER
            var dto = MakeDto(Guid.NewGuid(), Guid.NewGuid(),
                checkIn: Tomorrow, checkOut: Tomorrow);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreateReservationAsync_WithZeroRooms_ThrowsValidationException()
        {
            // Arrange
            var dto = MakeDto(Guid.NewGuid(), Guid.NewGuid(), rooms: 0);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CreateReservationAsync — Business Logic Tests
        // FIX: All GetQueryable() setups use ToMockQueryable()
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateReservationAsync_WithInvalidRoomType_ThrowsNotFoundException()
        {
            // Arrange: empty room type list — none belong to this hotel
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var dto = MakeDto(hotelId, roomTypeId);

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>()));

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreateReservationAsync_WhenRequestedRoomsExceedActiveRooms_ThrowsInsufficientInventoryException()
        {
            // Arrange: requesting 3 rooms but only 1 active physical room exists
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var dto = MakeDto(hotelId, roomTypeId, rooms: 3);

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            // FIX: .BuildMock() — only 1 active room
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room { RoomId = Guid.NewGuid(), RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                         }));

            // Act & Assert
            await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreateReservationAsync_WhenInventoryMissingForDate_ThrowsInsufficientInventoryException()
        {
            // Arrange: 2-night stay but inventory only configured for night 1 — night 2 missing
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var checkIn = Tomorrow;
            var checkOut = Today.AddDays(3);
            var dto = MakeDto(hotelId, roomTypeId, checkIn: checkIn, checkOut: checkOut);

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            // FIX: .BuildMock()
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room { RoomId = Guid.NewGuid(), RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                         }));

            // FIX: .BuildMock() — only 1 inventory record, but 2 nights needed
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory>
                              {
                                  new RoomTypeInventory
                                  {
                                      RoomTypeId        = roomTypeId,
                                      Date              = checkIn,
                                      TotalInventory    = 5,
                                      ReservedInventory = 0
                                      // Night 2 is MISSING
                                  }
                              }));

            // Act & Assert
            await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
        }
        [Fact]
        public async Task CreateReservationAsync_WhenInventoryFullOnADate_ThrowsInsufficientInventoryException()
        {
            // Arrange: night 1 fully booked (TotalInventory == ReservedInventory), night 2 available
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var checkIn = Tomorrow;
            var checkOut = Today.AddDays(3);
            var dto = MakeDto(hotelId, roomTypeId, checkIn: checkIn, checkOut: checkOut);

            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                         new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                     new Room { RoomId = Guid.NewGuid(), RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                         }));

            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory>
                              {
                          new RoomTypeInventory
                          {
                              RoomTypeId        = roomTypeId,
                              Date              = checkIn,
                              TotalInventory    = 2,
                              ReservedInventory = 2   // 0 available — fully booked!
                          },
                          new RoomTypeInventory
                          {
                              RoomTypeId        = roomTypeId,
                              Date              = checkIn.AddDays(1),
                              TotalInventory    = 5,
                              ReservedInventory = 0
                          }
                              }));

            // ← ADD THIS — service fetches rates BEFORE checking inventory per date
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(BuildRates(roomTypeId, checkIn, checkOut)));

            // Act & Assert
            await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreateReservationAsync_WhenNoRateConfigured_ThrowsRateNotFoundException()
        {
            // Arrange: inventory exists but no pricing configured
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var checkIn = Tomorrow;
            var checkOut = Today.AddDays(2);
            var dto = MakeDto(hotelId, roomTypeId, checkIn: checkIn, checkOut: checkOut);

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            // FIX: .BuildMock()
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                         }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(BuildInventory(roomTypeId, checkIn, checkOut)));

            // FIX: .BuildMock() — empty rates list
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<RoomTypeRate>()));

            // Act & Assert
            await Assert.ThrowsAsync<RateNotFoundException>(() =>
                _sut.CreateReservationAsync(Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreateReservationAsync_ValidRequest_ReturnsReservationWithCorrectTotalAmount()
        {
            // Arrange: 2 nights × 1 room × ₹1000/night = ₹2000 total
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var checkIn = Tomorrow;
            var checkOut = Today.AddDays(3);  // 2 nights
            var dto = MakeDto(hotelId, roomTypeId, checkIn: checkIn, checkOut: checkOut);

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            // FIX: .BuildMock()
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room
                             {
                                 RoomId     = roomId,
                                 RoomNumber = "101",
                                 Floor      = 1,
                                 RoomTypeId = roomTypeId,
                                 HotelId    = hotelId,
                                 IsActive   = true
                             }
                         }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(BuildInventory(roomTypeId, checkIn, checkOut)));

            // FIX: .BuildMock()
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(BuildRates(roomTypeId, checkIn, checkOut, 1000m)));

            _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>()))
                                .ReturnsAsync((Reservation res) => res);
            _reservationRoomRepoMock.Setup(r => r.AddAsync(It.IsAny<ReservationRoom>()))
                                    .ReturnsAsync((ReservationRoom rr) => rr);

            // Act
            var result = await _sut.CreateReservationAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            result.TotalAmount.Should().Be(2000m);         // 2 nights × ₹1000
            result.ReservationCode.Should().StartWith("RES-");
            result.Status.Should().Be("Pending");
            result.Rooms.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateReservationAsync_ValidRequest_DecrementsInventory()
        {
            // Arrange: booking 1 room for 1 night — ReservedInventory must go from 0 to 1
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var checkIn = Tomorrow;
            var checkOut = Today.AddDays(2);
            var dto = MakeDto(hotelId, roomTypeId, checkIn: checkIn, checkOut: checkOut);

            var inventoryRecord = new RoomTypeInventory
            {
                RoomTypeId = roomTypeId,
                Date = checkIn,
                TotalInventory = 5,
                ReservedInventory = 0  // Starts at 0
            };

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            // FIX: .BuildMock()
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room
                             {
                                 RoomId     = roomId,
                                 RoomNumber = "101",
                                 Floor      = 1,
                                 RoomTypeId = roomTypeId,
                                 HotelId    = hotelId,
                                 IsActive   = true
                             }
                         }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory> { inventoryRecord }));

            // FIX: .BuildMock()
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(BuildRates(roomTypeId, checkIn, checkOut)));

            _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>()))
                                .ReturnsAsync((Reservation res) => res);
            _reservationRoomRepoMock.Setup(r => r.AddAsync(It.IsAny<ReservationRoom>()))
                                    .ReturnsAsync((ReservationRoom rr) => rr);

            // Act
            await _sut.CreateReservationAsync(userId, dto);

            // Assert: ReservedInventory must be 1 (was 0, booked 1 room)
            inventoryRecord.ReservedInventory.Should().Be(1);
        }

        [Fact]
        public async Task CreateReservationAsync_WithSelectedRoomIds_HonorsGuestSelection()
        {
            // Arrange: guest explicitly selects room 101, not 102
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var selectedRoomId = Guid.NewGuid();
            var otherRoomId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var checkIn = Tomorrow;
            var checkOut = Today.AddDays(2);
            var dto = MakeDto(hotelId, roomTypeId, checkIn: checkIn, checkOut: checkOut,
                                    rooms: 1, selectedRoomIds: new List<Guid> { selectedRoomId });

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            // FIX: .BuildMock()
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room { RoomId = selectedRoomId, RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true },
                             new Room { RoomId = otherRoomId,    RoomNumber = "102", Floor = 1, RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                         }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(BuildInventory(roomTypeId, checkIn, checkOut)));

            // FIX: .BuildMock()
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(BuildRates(roomTypeId, checkIn, checkOut)));

            _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>()))
                                .ReturnsAsync((Reservation res) => res);
            _reservationRoomRepoMock.Setup(r => r.AddAsync(It.IsAny<ReservationRoom>()))
                                    .ReturnsAsync((ReservationRoom rr) => rr);

            // Act
            var result = await _sut.CreateReservationAsync(userId, dto);

            // Assert: only selected room included, other room excluded
            result.Rooms.Should().ContainSingle(r => r.RoomId == selectedRoomId);
            result.Rooms.Should().NotContain(r => r.RoomId == otherRoomId);
        }

        [Fact]
        public async Task CreateReservationAsync_SetsExpiryTime10MinutesFromNow()
        {
            // Arrange: capture the Reservation entity to check its ExpiryTime
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var checkIn = Tomorrow;
            var checkOut = Today.AddDays(2);
            var dto = MakeDto(hotelId, roomTypeId, checkIn: checkIn, checkOut: checkOut);
            Reservation? capturedReservation = null;

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId, IsActive = true }
                             }));

            // FIX: .BuildMock()
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room
                             {
                                 RoomId     = Guid.NewGuid(),
                                 RoomNumber = "101",
                                 Floor      = 1,
                                 RoomTypeId = roomTypeId,
                                 HotelId    = hotelId,
                                 IsActive   = true
                             }
                         }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(BuildInventory(roomTypeId, checkIn, checkOut)));

            // FIX: .BuildMock()
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(BuildRates(roomTypeId, checkIn, checkOut)));

            _reservationRepoMock.Setup(r => r.AddAsync(It.IsAny<Reservation>()))
                                .Callback<Reservation>(r => capturedReservation = r)
                                .ReturnsAsync((Reservation res) => res);
            _reservationRoomRepoMock.Setup(r => r.AddAsync(It.IsAny<ReservationRoom>()))
                                    .ReturnsAsync((ReservationRoom rr) => rr);

            // Act
            await _sut.CreateReservationAsync(userId, dto);

            // Assert: ExpiryTime must be ~10 minutes from now
            capturedReservation.Should().NotBeNull();
            capturedReservation!.ExpiryTime.Should().BeCloseTo(
                DateTime.UtcNow.AddMinutes(10),
                precision: TimeSpan.FromSeconds(5));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CancelReservationAsync Tests
        // FIX: GetQueryable() setups use ToMockQueryable()
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CancelReservationAsync_AlreadyCancelledReservation_ThrowsReservationFailedException()
        {
            // Arrange: reservation is already cancelled — cannot cancel again
            var reservationCode = "RES-ABCD1234";
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            var reservation = new Reservation
            {
                ReservationCode = reservationCode,
                UserId = userId,
                Status = ReservationStatus.Cancelled,
                CheckInDate = Tomorrow,
                CheckOutDate = DayAfter,
                ReservationRooms = new List<ReservationRoom> { new ReservationRoom { RoomTypeId = roomTypeId } },
                Transactions = new List<Transaction>()
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // Act & Assert
            await Assert.ThrowsAsync<ReservationFailedException>(() =>
                _sut.CancelReservationAsync(userId, reservationCode, "changed mind"));
        }

        [Fact]
        public async Task CancelReservationAsync_CompletedReservation_ThrowsValidationException()
        {
            // Arrange: cannot cancel a completed stay
            var reservationCode = "RES-DONE1234";
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            var reservation = new Reservation
            {
                ReservationCode = reservationCode,
                UserId = userId,
                Status = ReservationStatus.Completed,
                CheckInDate = Tomorrow,
                CheckOutDate = DayAfter,
                ReservationRooms = new List<ReservationRoom> { new ReservationRoom { RoomTypeId = roomTypeId } },
                Transactions = new List<Transaction>()
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _sut.CancelReservationAsync(userId, reservationCode, "reason"));
        }

        [Fact]
        public async Task CancelReservationAsync_PaidReservation_CreatesRefundRequest()
        {
            // Arrange: reservation is paid (has Success transaction) — cancelling must trigger refund request
            var reservationCode = "RES-PAID1234";
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var reservationId = Guid.NewGuid();

            var reservation = new Reservation
            {
                ReservationId = reservationId,
                ReservationCode = reservationCode,
                UserId = userId,
                Status = ReservationStatus.Confirmed,
                CheckInDate = Tomorrow,
                CheckOutDate = DayAfter,
                ReservationRooms = new List<ReservationRoom> { new ReservationRoom { RoomTypeId = roomTypeId } },
                Transactions = new List<Transaction>
                {
                    new Transaction { Status = PaymentStatus.Success }
                }
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(BuildInventory(roomTypeId, Tomorrow, DayAfter, 5, 1)));

            _refundRequestServiceMock.Setup(s => s.CreateRefundRequestAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.CancelReservationAsync(userId, reservationCode, "trip cancelled");

            // Assert: refund request created with correct reservation id and reason
            _refundRequestServiceMock.Verify(s => s.CreateRefundRequestAsync(
                reservationId, userId, "trip cancelled"), Times.Once);
        }

        [Fact]
        public async Task CancelReservationAsync_UnpaidReservation_DoesNotCreateRefundRequest()
        {
            // Arrange: reservation never paid — no refund request needed
            var reservationCode = "RES-FREE1234";
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            var reservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                ReservationCode = reservationCode,
                UserId = userId,
                Status = ReservationStatus.Pending,
                CheckInDate = Tomorrow,
                CheckOutDate = DayAfter,
                ReservationRooms = new List<ReservationRoom> { new ReservationRoom { RoomTypeId = roomTypeId } },
                Transactions = new List<Transaction>()  // No payment
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(BuildInventory(roomTypeId, Tomorrow, DayAfter, 5, 1)));

            // Act
            await _sut.CancelReservationAsync(userId, reservationCode, "changed mind");

            // Assert: NO refund request because guest never paid
            _refundRequestServiceMock.Verify(s => s.CreateRefundRequestAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CancelReservationAsync_RestoresInventory()
        {
            // Arrange: 2 rooms reserved, cancelling 1-room booking should bring it down to 1
            var reservationCode = "RES-RESTORE";
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            var inventoryRecord = new RoomTypeInventory
            {
                RoomTypeId = roomTypeId,
                Date = Tomorrow,
                TotalInventory = 5,
                ReservedInventory = 2  // 2 rooms were reserved
            };

            var reservation = new Reservation
            {
                ReservationCode = reservationCode,
                UserId = userId,
                Status = ReservationStatus.Pending,
                CheckInDate = Tomorrow,
                CheckOutDate = DayAfter,
                ReservationRooms = new List<ReservationRoom>
                {
                    new ReservationRoom { RoomTypeId = roomTypeId }  // 1 room
                },
                Transactions = new List<Transaction>()
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory> { inventoryRecord }));

            // Act
            await _sut.CancelReservationAsync(userId, reservationCode, "reason");

            // Assert: freed 1 room → 2 - 1 = 1
            inventoryRecord.ReservedInventory.Should().Be(1);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CompleteReservationAsync Tests
        // These use FirstOrDefaultAsync (not GetQueryable) — no BuildMock needed
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CompleteReservationAsync_ConfirmedReservation_SetsStatusAndCheckedIn()
        {
            // Arrange
            var code = "RES-CONF1234";
            var reservation = new Reservation
            {
                ReservationCode = code,
                Status = ReservationStatus.Confirmed,
                IsCheckedIn = false
            };

            _reservationRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Reservation, bool>>>()))
                                .ReturnsAsync(reservation);

            // Act
            var result = await _sut.CompleteReservationAsync(code);

            // Assert
            result.Should().BeTrue();
            reservation.Status.Should().Be(ReservationStatus.Completed);
            reservation.IsCheckedIn.Should().BeTrue();  // Completion implies check-in
        }

        [Fact]
        public async Task CompleteReservationAsync_PendingReservation_ThrowsValidationException()
        {
            // Arrange: cannot complete a Pending (unpaid) reservation
            var reservation = new Reservation
            {
                ReservationCode = "RES-PEND",
                Status = ReservationStatus.Pending
            };

            _reservationRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Reservation, bool>>>()))
                                .ReturnsAsync(reservation);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _sut.CompleteReservationAsync("RES-PEND"));
        }
    }
}