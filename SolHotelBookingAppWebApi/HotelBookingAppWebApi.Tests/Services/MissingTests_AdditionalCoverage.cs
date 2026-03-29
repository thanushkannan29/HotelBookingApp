using Xunit;
using Moq;
using MockQueryable.Moq;
using FluentAssertions;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Dashboard;
using HotelBookingAppWebApi.Models.DTOs.Reservation;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Exceptions;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Tests.Services
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // GAP ANALYSIS — WHAT WAS MISSING FROM YOUR EXISTING TEST FILES
    // ═══════════════════════════════════════════════════════════════════════════════
    //
    // After reviewing all 6 uploaded test files, these scenarios were NOT covered:
    //
    // 1. DashboardService — 0 tests existed (GetAdminDashboard, GetGuestDashboard,
    //    GetSuperAdminDashboard)
    //
    // 2. ReservationService.GetAvailableRoomsAsync — not tested
    //    (finds rooms NOT already booked for given dates)
    //
    // 3. HotelService.GetAllHotelsForSuperAdminAsync — not tested
    //    (the optimised 3-query N+1 fix)
    //
    // 4. ReservationService.GetHotelReservationsAsync — not tested
    //    (admin viewing their hotel's reservations)
    //
    // 5. TransactionService.GetAllTransactionsAsync — Admin role not tested
    //    (only Guest and SuperAdmin were tested)
    //
    // 6. UserService.GetBookingHistoryAsync — not tested (paginated booking history)
    //
    // This file adds tests for all 6 gaps.
    // ═══════════════════════════════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════════════════════════════
    // GAP 1: DASHBOARD SERVICE TESTS
    // 0 tests existed — this is a completely untested service
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for DashboardService.
    /// All 3 dashboard methods return aggregated statistics. We verify:
    ///   - Correct hotel is found for the admin's userId
    ///   - Counts and revenue are calculated from correct filtered data
    ///   - SuperAdmin dashboard sums across all hotels
    /// </summary>
    public class DashboardServiceTests
    {
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
        private readonly Mock<IRepository<Guid, Review>> _reviewRepoMock = new();
        private readonly Mock<IRepository<Guid, Room>> _roomRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
        private readonly Mock<IRepository<Guid, RefundRequest>> _refundRepoMock = new();
        private readonly DashboardService _sut;

        public DashboardServiceTests()
        {
            _sut = new DashboardService(
                _userRepoMock.Object,
                _hotelRepoMock.Object,
                _reservationRepoMock.Object,
                _transactionRepoMock.Object,
                _reviewRepoMock.Object,
                _roomRepoMock.Object,
                _roomTypeRepoMock.Object,
                _refundRepoMock.Object);
        }

        private static IQueryable<T> Q<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        // ── ADMIN DASHBOARD ────────────────────────────────────────────────────

        [Fact]
        public async Task GetAdminDashboardAsync_ReturnsCorrectHotelName()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<User>
                         {
                             new User { UserId = adminId, HotelId = hotelId }
                         }));

            _hotelRepoMock.Setup(r => r.GetAsync(hotelId))
                          .ReturnsAsync(new Hotel
                          {
                              HotelId = hotelId,
                              Name = "Grand Palace Hotel",
                              IsActive = true,
                              IsBlockedBySuperAdmin = false
                          });

            // Set up all count queries to return 0 so they don't throw
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<Room>()));
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(Q(new List<RoomType>()));
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Reservation>()));
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Transaction>()));
            _reviewRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<Review>()));
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<RefundRequest>()));

            // Act
            var result = await _sut.GetAdminDashboardAsync(adminId);

            // Assert
            result.Should().NotBeNull();
            result.HotelName.Should().Be("Grand Palace Hotel");
            result.HotelId.Should().Be(hotelId);
        }

        [Fact]
        public async Task GetAdminDashboardAsync_AdminWithNoHotel_ThrowsNotFoundException()
        {
            // Arrange: Admin has no HotelId linked
            var adminId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<User>
                         {
                             new User { UserId = adminId, HotelId = null }
                         }));

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.GetAdminDashboardAsync(adminId));
        }

        [Fact]
        public async Task GetAdminDashboardAsync_CountsActiveAndTotalRoomsCorrectly()
        {
            // Arrange: 3 rooms total, 2 active, 1 inactive
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<User>
                         {
                             new User { UserId = adminId, HotelId = hotelId }
                         }));

            _hotelRepoMock.Setup(r => r.GetAsync(hotelId))
                          .ReturnsAsync(new Hotel { HotelId = hotelId, Name = "Test Hotel" });

            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<Room>
                         {
                             new Room { HotelId = hotelId, IsActive = true },
                             new Room { HotelId = hotelId, IsActive = true },
                             new Room { HotelId = hotelId, IsActive = false }  // Inactive
                         }));

            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(Q(new List<RoomType>()));
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Reservation>()));
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Transaction>()));
            _reviewRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<Review>()));
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<RefundRequest>()));

            // Act
            var result = await _sut.GetAdminDashboardAsync(adminId);

            // Assert
            result.TotalRooms.Should().Be(3);
            result.ActiveRooms.Should().Be(2);
        }

        [Fact]
        public async Task GetAdminDashboardAsync_RevenueOnlyCountsSuccessTransactions()
        {
            // Arrange: 1 success transaction (₹5000), 1 refunded (₹3000) — only ₹5000 counts
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<User>
                         {
                             new User { UserId = adminId, HotelId = hotelId }
                         }));

            _hotelRepoMock.Setup(r => r.GetAsync(hotelId))
                          .ReturnsAsync(new Hotel { HotelId = hotelId, Name = "Test Hotel" });

            _roomRepoMock.Setup(r => r.GetQueryable()).Returns(Q(new List<Room>()));
            _roomTypeRepoMock.Setup(r => r.GetQueryable()).Returns(Q(new List<RoomType>()));
            _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(Q(new List<Reservation>()));
            _reviewRepoMock.Setup(r => r.GetQueryable()).Returns(Q(new List<Review>()));
            _refundRepoMock.Setup(r => r.GetQueryable()).Returns(Q(new List<RefundRequest>()));

            _transactionRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Transaction>
                                {
                                    new Transaction
                                    {
                                        Amount     = 5000m,
                                        Status     = PaymentStatus.Success,
                                        Reservation = new Reservation { HotelId = hotelId }
                                    },
                                    new Transaction
                                    {
                                        Amount     = 3000m,
                                        Status     = PaymentStatus.Refunded, // Should NOT count
                                        Reservation = new Reservation { HotelId = hotelId }
                                    }
                                }));

            // Act
            var result = await _sut.GetAdminDashboardAsync(adminId);

            // Assert: Only Success transactions count as revenue
            result.TotalRevenue.Should().Be(5000m);
        }

        // ── GUEST DASHBOARD ────────────────────────────────────────────────────

        [Fact]
        public async Task GetGuestDashboardAsync_ReturnsCorrectBookingCounts()
        {
            // Arrange: Guest has 1 confirmed, 1 completed, 1 cancelled reservation
            var guestId = Guid.NewGuid();

            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Reservation>
                                {
                                    new Reservation { UserId = guestId, Status = ReservationStatus.Confirmed },
                                    new Reservation { UserId = guestId, Status = ReservationStatus.Completed },
                                    new Reservation { UserId = guestId, Status = ReservationStatus.Cancelled },
                                }));

            _transactionRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Transaction>()));

            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<RefundRequest>()));

            // Act
            var result = await _sut.GetGuestDashboardAsync(guestId);

            // Assert
            result.TotalBookings.Should().Be(3);
            result.ActiveBookings.Should().Be(1);      // Confirmed
            result.CompletedBookings.Should().Be(1);
            result.CancelledBookings.Should().Be(1);
        }

        [Fact]
        public async Task GetGuestDashboardAsync_TotalSpentOnlyCountsSuccessTransactions()
        {
            // Arrange: ₹2000 paid, ₹1000 refunded — TotalSpent should be ₹2000 only
            var guestId = Guid.NewGuid();

            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Reservation>()));

            _transactionRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Transaction>
                                {
                                    new Transaction
                                    {
                                        Amount     = 2000m,
                                        Status     = PaymentStatus.Success,
                                        Reservation = new Reservation { UserId = guestId }
                                    },
                                    new Transaction
                                    {
                                        Amount     = 1000m,
                                        Status     = PaymentStatus.Refunded, // Not counted
                                        Reservation = new Reservation { UserId = guestId }
                                    }
                                }));

            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<RefundRequest>()));

            // Act
            var result = await _sut.GetGuestDashboardAsync(guestId);

            // Assert
            result.TotalSpent.Should().Be(2000m);
        }

        // ── SUPERADMIN DASHBOARD ───────────────────────────────────────────────

        [Fact]
        public async Task GetSuperAdminDashboardAsync_ReturnsSystemWideCounts()
        {
            // Arrange: 3 hotels (2 active, 1 blocked), 5 users, 10 reservations
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(Q(new List<Hotel>
                          {
                              new Hotel { IsActive = true,  IsBlockedBySuperAdmin = false },
                              new Hotel { IsActive = true,  IsBlockedBySuperAdmin = false },
                              new Hotel { IsActive = false, IsBlockedBySuperAdmin = true  },
                          }));

            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(Enumerable.Range(1, 5)
                             .Select(_ => new User()).ToList()));

            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(Enumerable.Range(1, 10)
                                    .Select(_ => new Reservation()).ToList()));

            _transactionRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Transaction>()));

            _reviewRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<Review>()));

            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(Q(new List<RefundRequest>()));

            // Act
            var result = await _sut.GetSuperAdminDashboardAsync();

            // Assert
            result.TotalHotels.Should().Be(3);
            result.ActiveHotels.Should().Be(2);
            result.BlockedHotels.Should().Be(1);
            result.TotalUsers.Should().Be(5);
            result.TotalReservations.Should().Be(10);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // GAP 2: RESERVATION SERVICE — GetAvailableRoomsAsync (not tested)
    // GAP 4: RESERVATION SERVICE — GetHotelReservationsAsync (not tested)
    // These are added to a supplemental class
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Additional tests for ReservationService methods that were not covered.
    /// GetAvailableRoomsAsync: finds rooms not booked during requested dates.
    /// GetHotelReservationsAsync: admin views paginated list of hotel reservations.
    /// </summary>
    public class ReservationServiceAdditionalTests
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

        public ReservationServiceAdditionalTests()
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

        private static IQueryable<T> Q<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        private static DateOnly Tomorrow => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        private static DateOnly DayAfter => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        // ── GetAvailableRoomsAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetAvailableRoomsAsync_ExcludesAlreadyBookedRooms()
        {
            // Arrange: room 101 is booked for Tomorrow→DayAfter, room 102 is free
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var bookedRoomId = Guid.NewGuid();
            var freeRoomId = Guid.NewGuid();

            // The ReservationRoom for the already-booked room
            _reservationRoomRepoMock.Setup(r => r.GetQueryable())
                                    .Returns(Q(new List<ReservationRoom>
                                    {
                                        new ReservationRoom
                                        {
                                            RoomId      = bookedRoomId,
                                            RoomTypeId  = roomTypeId,
                                            Reservation = new Reservation
                                            {
                                                HotelId      = hotelId,
                                                Status       = ReservationStatus.Confirmed,
                                                CheckInDate  = Tomorrow,
                                                CheckOutDate = DayAfter
                                            }
                                        }
                                    }));

            // Two active rooms — one is booked, one is free
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<Room>
                         {
                             new Room
                             {
                                 RoomId     = bookedRoomId,
                                 RoomNumber = "101",
                                 Floor      = 1,
                                 HotelId    = hotelId,
                                 RoomTypeId = roomTypeId,
                                 IsActive   = true,
                                 RoomType   = new RoomType { Name = "Standard" }
                             },
                             new Room
                             {
                                 RoomId     = freeRoomId,
                                 RoomNumber = "102",
                                 Floor      = 1,
                                 HotelId    = hotelId,
                                 RoomTypeId = roomTypeId,
                                 IsActive   = true,
                                 RoomType   = new RoomType { Name = "Standard" }
                             }
                         }));

            // Act
            var result = await _sut.GetAvailableRoomsAsync(hotelId, roomTypeId, Tomorrow, DayAfter);

            // Assert: Only the free room (102) should be returned
            var available = result.ToList();
            available.Should().HaveCount(1);
            available.First().RoomId.Should().Be(freeRoomId);
            available.First().RoomNumber.Should().Be("102");
        }

        [Fact]
        public async Task GetAvailableRoomsAsync_WhenNoBookings_ReturnsAllActiveRooms()
        {
            // Arrange: No existing reservations — all rooms should be available
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            // No booked rooms
            _reservationRoomRepoMock.Setup(r => r.GetQueryable())
                                    .Returns(Q(new List<ReservationRoom>()));

            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<Room>
                         {
                             new Room { RoomId = Guid.NewGuid(), RoomNumber = "101", Floor = 1, HotelId = hotelId, RoomTypeId = roomTypeId, IsActive = true, RoomType = new RoomType { Name = "Deluxe" } },
                             new Room { RoomId = Guid.NewGuid(), RoomNumber = "102", Floor = 1, HotelId = hotelId, RoomTypeId = roomTypeId, IsActive = true, RoomType = new RoomType { Name = "Deluxe" } },
                             new Room { RoomId = Guid.NewGuid(), RoomNumber = "103", Floor = 1, HotelId = hotelId, RoomTypeId = roomTypeId, IsActive = false, RoomType = new RoomType { Name = "Deluxe" } }, // Inactive
                         }));

            // Act
            var result = await _sut.GetAvailableRoomsAsync(hotelId, roomTypeId, Tomorrow, DayAfter);

            // Assert: Only 2 active rooms returned (inactive room excluded)
            result.Should().HaveCount(2);
        }

        // ── GetHotelReservationsAsync ──────────────────────────────────────────

        [Fact]
        public async Task GetHotelReservationsAsync_AdminWithNoHotel_ThrowsUnAuthorizedException()
        {
            // Arrange: Admin exists but has no HotelId linked
            var adminId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = null }); // No hotel!

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.GetHotelReservationsAsync(adminId, 1, 10));
        }

        [Fact]
        public async Task GetHotelReservationsAsync_ReturnsPagedResultsForHotel()
        {
            // Arrange: Admin linked to hotel with 3 reservations
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            var reservations = new List<Reservation>
            {
                new Reservation
                {
                    ReservationId   = Guid.NewGuid(),
                    ReservationCode = "RES-001",
                    HotelId         = hotelId,
                    Status          = ReservationStatus.Confirmed,
                    CheckInDate     = Tomorrow,
                    CheckOutDate    = DayAfter,
                    CreatedDate     = DateTime.UtcNow,
                    Hotel           = new Hotel { Name = "Test Hotel" },
                    ReservationRooms = new List<ReservationRoom>
                    {
                        new ReservationRoom
                        {
                            RoomTypeId = Guid.NewGuid(),
                            Room       = new Room { RoomNumber = "101", Floor = 1 },
                            RoomType   = new RoomType { Name = "Standard" }
                        }
                    }
                },
                new Reservation
                {
                    ReservationId   = Guid.NewGuid(),
                    ReservationCode = "RES-002",
                    HotelId         = hotelId,
                    Status          = ReservationStatus.Pending,
                    CheckInDate     = Tomorrow,
                    CheckOutDate    = DayAfter,
                    CreatedDate     = DateTime.UtcNow,
                    Hotel           = new Hotel { Name = "Test Hotel" },
                    ReservationRooms = new List<ReservationRoom>
                    {
                        new ReservationRoom
                        {
                            RoomTypeId = Guid.NewGuid(),
                            Room       = new Room { RoomNumber = "102", Floor = 1 },
                            RoomType   = new RoomType { Name = "Deluxe" }
                        }
                    }
                }
            };

            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(reservations));

            // Act
            var result = await _sut.GetHotelReservationsAsync(adminId, 1, 10);

            // Assert
            result.TotalCount.Should().Be(2);
            result.Reservations.Should().HaveCount(2);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // GAP 3: HOTEL SERVICE — GetAllHotelsForSuperAdminAsync (not tested)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests the N+1 fix in HotelService.GetAllHotelsForSuperAdminAsync.
    /// Uses 3 total queries (hotels, reservation counts, revenue) merged in memory.
    /// </summary>
    public class HotelServiceSuperAdminTests
    {
        private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
        private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly HotelService _sut;

        public HotelServiceSuperAdminTests()
        {
            _sut = new HotelService(
                _hotelRepoMock.Object,
                _userRepoMock.Object,
                _roomTypeRepoMock.Object,
                _transactionRepoMock.Object,
                _reservationRepoMock.Object,
                _auditLogServiceMock.Object,
                _unitOfWorkMock.Object);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _auditLogServiceMock.Setup(a => a.LogAsync(
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        private static IQueryable<T> Q<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        [Fact]
        public async Task GetAllHotelsForSuperAdminAsync_ReturnsAllHotelsWithStats()
        {
            // Arrange
            var hotel1Id = Guid.NewGuid();
            var hotel2Id = Guid.NewGuid();

            var hotels = new List<Hotel>
            {
                new Hotel { HotelId = hotel1Id, Name = "Alpha Hotel", City = "Mumbai", IsActive = true, IsBlockedBySuperAdmin = false, CreatedAt = DateTime.UtcNow },
                new Hotel { HotelId = hotel2Id, Name = "Beta Hotel",  City = "Delhi",  IsActive = false, IsBlockedBySuperAdmin = true,  CreatedAt = DateTime.UtcNow }
            };

            var reservations = new List<Reservation>
            {
                new Reservation { HotelId = hotel1Id },
                new Reservation { HotelId = hotel1Id },  // 2 reservations for hotel1
                new Reservation { HotelId = hotel2Id },  // 1 reservation for hotel2
            };

            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    Amount     = 5000m,
                    Status     = PaymentStatus.Success,
                    Reservation = new Reservation { HotelId = hotel1Id }
                }
                // hotel2 has no revenue
            };

            _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(Q(hotels));
            _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(Q(reservations));
            _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(Q(transactions));

            // Act
            var result = (await _sut.GetAllHotelsForSuperAdminAsync()).ToList();

            // Assert
            result.Should().HaveCount(2);

            var hotel1Result = result.First(h => h.HotelId == hotel1Id);
            hotel1Result.TotalReservations.Should().Be(2);
            hotel1Result.TotalRevenue.Should().Be(5000m);
            hotel1Result.IsActive.Should().BeTrue();

            var hotel2Result = result.First(h => h.HotelId == hotel2Id);
            hotel2Result.TotalReservations.Should().Be(1);
            hotel2Result.TotalRevenue.Should().Be(0m);
            hotel2Result.IsBlockedBySuperAdmin.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllHotelsForSuperAdminAsync_HotelWithNoReservations_ShowsZeroCounts()
        {
            // Arrange: Hotel exists but has no bookings or revenue
            var hotelId = Guid.NewGuid();
            var hotels = new List<Hotel>
            {
                new Hotel { HotelId = hotelId, Name = "Empty Hotel", City = "Chennai", IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            _hotelRepoMock.Setup(r => r.GetQueryable()).Returns(Q(hotels));
            _reservationRepoMock.Setup(r => r.GetQueryable()).Returns(Q(new List<Reservation>()));
            _transactionRepoMock.Setup(r => r.GetQueryable()).Returns(Q(new List<Transaction>()));

            // Act
            var result = (await _sut.GetAllHotelsForSuperAdminAsync()).ToList();

            // Assert: Zero counts for a hotel with no bookings
            result.Should().HaveCount(1);
            result.First().TotalReservations.Should().Be(0);
            result.First().TotalRevenue.Should().Be(0m);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // GAP 5: TRANSACTION SERVICE — Admin role filtering (not tested)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The existing TransactionServiceTests tested Guest and SuperAdmin roles.
    /// Admin role filtering was missing — Admin should only see their hotel's transactions.
    /// </summary>
    public class TransactionServiceAdminRoleTests
    {
        private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly TransactionService _sut;

        public TransactionServiceAdminRoleTests()
        {
            _sut = new TransactionService(
                _transactionRepoMock.Object,
                _reservationRepoMock.Object,
                _inventoryRepoMock.Object,
                _userRepoMock.Object,
                _unitOfWorkMock.Object);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        }

        private static IQueryable<T> Q<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        [Fact]
        public async Task GetAllTransactionsAsync_AdminRole_ReturnsOnlyHotelTransactions()
        {
            // Arrange: Admin's hotel has 2 transactions, another hotel has 1
            var adminId = Guid.NewGuid();
            var adminHotelId = Guid.NewGuid();
            var otherHotelId = Guid.NewGuid();

            // Admin's user record has HotelId set
            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(Q(new List<User>
                         {
                             new User { UserId = adminId, HotelId = adminHotelId }
                         }));

            var transactions = new List<Transaction>
            {
                new Transaction { TransactionId = Guid.NewGuid(), TransactionDate = DateTime.UtcNow, Reservation = new Reservation { HotelId = adminHotelId } },
                new Transaction { TransactionId = Guid.NewGuid(), TransactionDate = DateTime.UtcNow, Reservation = new Reservation { HotelId = adminHotelId } },
                new Transaction { TransactionId = Guid.NewGuid(), TransactionDate = DateTime.UtcNow, Reservation = new Reservation { HotelId = otherHotelId } }, // Other hotel
            };

            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(Q(transactions));

            // Act
            var result = await _sut.GetAllTransactionsAsync(adminId, "Admin", 1, 10);

            // Assert: Admin sees only their hotel's 2 transactions
            result.TotalCount.Should().Be(2);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // GAP 6: USER SERVICE — GetBookingHistoryAsync (not tested)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for UserService.GetBookingHistoryAsync — paginated booking history.
    /// This was completely missing from the original test file.
    /// </summary>
    public class UserServiceBookingHistoryTests
    {
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly UserService _sut;

        public UserServiceBookingHistoryTests()
        {
            _sut = new UserService(
                _userRepoMock.Object,
                _reservationRepoMock.Object,
                _unitOfWorkMock.Object);
        }

        private static IQueryable<T> Q<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        [Fact]
        public async Task GetBookingHistoryAsync_ReturnsPagedBookings()
        {
            // Arrange: Guest has 3 reservations
            var userId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            var reservations = new List<Reservation>
            {
                new Reservation
                {
                    ReservationId   = Guid.NewGuid(),
                    ReservationCode = "RES-001",
                    UserId          = userId,
                    HotelId         = hotelId,
                    CheckInDate     = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                    CheckOutDate    = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
                    TotalAmount     = 2000m,
                    Status          = ReservationStatus.Confirmed,
                    CreatedDate     = DateTime.UtcNow,
                    Hotel           = new Hotel { Name = "Grand Hotel" }
                },
                new Reservation
                {
                    ReservationId   = Guid.NewGuid(),
                    ReservationCode = "RES-002",
                    UserId          = userId,
                    HotelId         = hotelId,
                    CheckInDate     = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                    CheckOutDate    = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    TotalAmount     = 3000m,
                    Status          = ReservationStatus.Completed,
                    CreatedDate     = DateTime.UtcNow.AddDays(-5),
                    Hotel           = new Hotel { Name = "Grand Hotel" }
                }
            };

            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(reservations));

            // Act
            var result = await _sut.GetBookingHistoryAsync(userId, page: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Bookings.Should().HaveCount(2);
            result.Bookings.First().HotelName.Should().Be("Grand Hotel");
        }

        [Fact]
        public async Task GetBookingHistoryAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange: 5 reservations — page 1 with pageSize 2 should return 2
            var userId = Guid.NewGuid();

            var reservations = Enumerable.Range(1, 5).Select(i => new Reservation
            {
                ReservationId = Guid.NewGuid(),
                ReservationCode = $"RES-00{i}",
                UserId = userId,
                CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i)),
                CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i + 1)),
                TotalAmount = 1000m * i,
                Status = ReservationStatus.Confirmed,
                CreatedDate = DateTime.UtcNow.AddDays(-i),
                Hotel = new Hotel { Name = $"Hotel {i}" }
            }).ToList();

            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(reservations));

            // Act: Request page 1 with pageSize 2
            var result = await _sut.GetBookingHistoryAsync(userId, page: 1, pageSize: 2);

            // Assert: TotalCount = 5 (all), but only 2 on this page
            result.TotalCount.Should().Be(5);
            result.Bookings.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetBookingHistoryAsync_GuestWithNoBookings_ReturnsEmptyList()
        {
            // Arrange: No reservations for this guest
            var userId = Guid.NewGuid();

            _reservationRepoMock.Setup(r => r.GetQueryable())
                                .Returns(Q(new List<Reservation>()));

            // Act
            var result = await _sut.GetBookingHistoryAsync(userId, page: 1, pageSize: 10);

            // Assert
            result.TotalCount.Should().Be(0);
            result.Bookings.Should().BeEmpty();
        }
    }
}