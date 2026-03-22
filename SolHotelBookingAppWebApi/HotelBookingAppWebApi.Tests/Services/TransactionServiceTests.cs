using Xunit;
using Moq;
using MockQueryable.Moq;          // ← ADDED: gives .BuildMock() on any List<T>
using FluentAssertions;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Transactions;
using HotelBookingAppWebApi.Exceptions;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Tests.Services
{
    /// <summary>
    /// Tests for TransactionService.
    /// Key rules:
    ///   - Payment: Pending→Confirmed atomically, cannot pay twice, cannot pay expired/cancelled
    ///   - Direct refund: Only within 30 minutes of payment. Backend enforces the window.
    ///   - Get all: Role-based filtering (Guest=own, Admin=hotel's, SuperAdmin=all)
    ///
    /// FIX APPLIED:
    ///   All .AsQueryable() replaced with .AsQueryable().BuildMock()
    ///   so EF Core async methods (FirstOrDefaultAsync, ToListAsync, CountAsync) work.
    ///   Requires NuGet: MockQueryable.Moq version 7.0.0
    /// </summary>
    public class TransactionServiceTests
    {
        private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly TransactionService _sut;

        public TransactionServiceTests()
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

        // ── HELPER ────────────────────────────────────────────────────────────
        /// <summary>
        /// Wraps a list into a BuildMock() queryable so EF Core async LINQ
        /// methods (FirstOrDefaultAsync, ToListAsync, CountAsync, AnyAsync)
        /// work without a real database connection.
        /// </summary>
        private static IQueryable<T> ToMockQueryable<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        // ═══════════════════════════════════════════════════════════════════════
        // CreatePaymentAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreatePaymentAsync_ValidPendingReservation_CreatesSuccessfulTransaction()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var amount = 2000m;
            var reservation = new Reservation
            {
                ReservationId = reservationId,
                TotalAmount = amount,
                Status = ReservationStatus.Pending,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),  // Still valid
                Transactions = new List<Transaction>()
            };
            var dto = new CreatePaymentDto
            {
                ReservationId = reservationId,
                PaymentMethod = PaymentMethod.UPI
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                                 .ReturnsAsync((Transaction t) => t);

            // Act
            var result = await _sut.CreatePaymentAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Amount.Should().Be(amount);
            result.Status.Should().Be(PaymentStatus.Success);       // Simulated gateway → always Success
            result.PaymentMethod.Should().Be(PaymentMethod.UPI);
            reservation.Status.Should().Be(ReservationStatus.Confirmed); // Atomically confirmed
        }

        [Fact]
        public async Task CreatePaymentAsync_CancelledReservation_ThrowsPaymentException()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var reservation = new Reservation
            {
                ReservationId = reservationId,
                Status = ReservationStatus.Cancelled,
                Transactions = new List<Transaction>()
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<PaymentException>(() =>
                _sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservationId }));
            ex.Message.Should().Contain("cancelled");
        }

        [Fact]
        public async Task CreatePaymentAsync_CompletedReservation_ThrowsPaymentException()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var reservation = new Reservation
            {
                ReservationId = reservationId,
                Status = ReservationStatus.Completed,
                Transactions = new List<Transaction>()
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // Act & Assert
            await Assert.ThrowsAsync<PaymentException>(() =>
                _sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservationId }));
        }

        [Fact]
        public async Task CreatePaymentAsync_ExpiredReservation_ThrowsPaymentException()
        {
            // Arrange: ExpiryTime is in the past — payment window closed
            var reservationId = Guid.NewGuid();
            var reservation = new Reservation
            {
                ReservationId = reservationId,
                Status = ReservationStatus.Pending,
                ExpiryTime = DateTime.UtcNow.AddMinutes(-1),  // Expired 1 minute ago
                Transactions = new List<Transaction>()
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<PaymentException>(() =>
                _sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservationId }));
            ex.Message.Should().Contain("expired");
        }

        [Fact]
        public async Task CreatePaymentAsync_AlreadyPaidReservation_ThrowsPaymentException()
        {
            // Arrange: already has a successful transaction
            var reservationId = Guid.NewGuid();
            var reservation = new Reservation
            {
                ReservationId = reservationId,
                Status = ReservationStatus.Confirmed,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                Transactions = new List<Transaction>
                {
                    new Transaction { Status = PaymentStatus.Success }  // Already paid!
                }
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<PaymentException>(() =>
                _sut.CreatePaymentAsync(new CreatePaymentDto { ReservationId = reservationId }));
            ex.Message.Should().Contain("already been paid");
        }

        [Fact]
        public async Task CreatePaymentAsync_ConfirmsReservationAtomicallyWithPayment()
        {
            // Arrange
            var reservationId = Guid.NewGuid();
            var reservation = new Reservation
            {
                ReservationId = reservationId,
                TotalAmount = 5000m,
                Status = ReservationStatus.Pending,
                ExpiryTime = DateTime.UtcNow.AddMinutes(8),
                Transactions = new List<Transaction>()
            };

            // FIX: .BuildMock()
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation> { reservation }));

            _transactionRepoMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                                 .ReturnsAsync((Transaction t) => t);

            // Act
            await _sut.CreatePaymentAsync(new CreatePaymentDto
            {
                ReservationId = reservationId,
                PaymentMethod = PaymentMethod.CreditCard
            });

            // Assert: reservation confirmed AND commit called — atomic operation
            reservation.Status.Should().Be(ReservationStatus.Confirmed);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DirectGuestRefundAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DirectGuestRefundAsync_WithinWindow_RefundsSuccessfully()
        {
            // Arrange: payment 10 minutes ago — inside the 30-minute window
            var transactionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var checkOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Success,
                TransactionDate = DateTime.UtcNow.AddMinutes(-10),  // 10 min ago
                Reservation = new Reservation
                {
                    UserId = userId,
                    Status = ReservationStatus.Confirmed,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    ReservationRooms = new List<ReservationRoom>
                    {
                        new ReservationRoom { RoomTypeId = roomTypeId }
                    },
                    Transactions = new List<Transaction>()
                }
            };

            // FIX: .BuildMock()
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Transaction> { transaction }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory>
                              {
                                  new RoomTypeInventory
                                  {
                                      RoomTypeId        = roomTypeId,
                                      Date              = checkIn,
                                      TotalInventory    = 5,
                                      ReservedInventory = 1
                                  }
                              }));

            // Act
            var result = await _sut.DirectGuestRefundAsync(
                transactionId, userId, new RefundRequestDto { Reason = "Changed my mind" });

            // Assert
            result.Status.Should().Be(PaymentStatus.Refunded);
            transaction.Reservation.Status.Should().Be(ReservationStatus.Cancelled);
        }

        [Fact]
        public async Task DirectGuestRefundAsync_After30Minutes_ThrowsPaymentException()
        {
            // Arrange: payment 31 minutes ago — OUTSIDE the 30-minute window
            var transactionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Success,
                TransactionDate = DateTime.UtcNow.AddMinutes(-31),  // 31 min ago = too late
                Reservation = new Reservation
                {
                    UserId = userId,
                    Status = ReservationStatus.Confirmed,
                    CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                    ReservationRooms = new List<ReservationRoom>
                    {
                        new ReservationRoom { RoomTypeId = roomTypeId }
                    },
                    Transactions = new List<Transaction>()
                }
            };

            // FIX: .BuildMock()
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Transaction> { transaction }));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<PaymentException>(() =>
                _sut.DirectGuestRefundAsync(transactionId, userId, new RefundRequestDto { Reason = "too late" }));
            ex.Message.Should().Contain("expired");
        }

        [Fact]
        public async Task DirectGuestRefundAsync_WrongUser_ThrowsUnAuthorizedException()
        {
            // Arrange: transaction owned by ownerUserId, attackerUserId tries to refund it
            var transactionId = Guid.NewGuid();
            var ownerUserId = Guid.NewGuid();
            var attackerUserId = Guid.NewGuid();  // Different user!
            var roomTypeId = Guid.NewGuid();

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Success,
                TransactionDate = DateTime.UtcNow.AddMinutes(-5),
                Reservation = new Reservation
                {
                    UserId = ownerUserId,  // Belongs to ownerUserId
                    Status = ReservationStatus.Confirmed,
                    CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                    ReservationRooms = new List<ReservationRoom>
                    {
                        new ReservationRoom { RoomTypeId = roomTypeId }
                    },
                    Transactions = new List<Transaction>()
                }
            };

            // FIX: .BuildMock()
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Transaction> { transaction }));

            // Act & Assert: attacker cannot refund someone else's transaction
            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.DirectGuestRefundAsync(
                    transactionId, attackerUserId, new RefundRequestDto { Reason = "hack attempt" }));
        }

        [Fact]
        public async Task DirectGuestRefundAsync_CompletedReservation_ThrowsPaymentException()
        {
            // Arrange: reservation is already completed — cannot refund
            var transactionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Success,
                TransactionDate = DateTime.UtcNow.AddMinutes(-5),  // Within window
                Reservation = new Reservation
                {
                    UserId = userId,
                    Status = ReservationStatus.Completed,  // Already completed
                    CheckInDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    CheckOutDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                    ReservationRooms = new List<ReservationRoom>
                    {
                        new ReservationRoom { RoomTypeId = roomTypeId }
                    },
                    Transactions = new List<Transaction>()
                }
            };

            // FIX: .BuildMock()
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Transaction> { transaction }));

            // Act & Assert
            await Assert.ThrowsAsync<PaymentException>(() =>
                _sut.DirectGuestRefundAsync(transactionId, userId, new RefundRequestDto { Reason = "done" }));
        }

        [Fact]
        public async Task DirectGuestRefundAsync_RestoresInventory()
        {
            // Arrange: 2 rooms reserved, refunding 1-room booking should bring it to 1
            var transactionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var checkOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

            var inventoryRecord = new RoomTypeInventory
            {
                RoomTypeId = roomTypeId,
                Date = checkIn,
                TotalInventory = 5,
                ReservedInventory = 2  // 2 rooms were reserved
            };

            var transaction = new Transaction
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Success,
                TransactionDate = DateTime.UtcNow.AddMinutes(-5),
                Reservation = new Reservation
                {
                    UserId = userId,
                    Status = ReservationStatus.Confirmed,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    ReservationRooms = new List<ReservationRoom>
                    {
                        new ReservationRoom { RoomTypeId = roomTypeId }  // 1 room
                    },
                    Transactions = new List<Transaction>()
                }
            };

            // FIX: .BuildMock()
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Transaction> { transaction }));

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory> { inventoryRecord }));

            // Act
            await _sut.DirectGuestRefundAsync(
                transactionId, userId, new RefundRequestDto { Reason = "refund test" });

            // Assert: 2 - 1 = 1 room remaining reserved
            inventoryRecord.ReservedInventory.Should().Be(1);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // GetAllTransactionsAsync Tests — Role-Based Filtering
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAllTransactionsAsync_GuestRole_ReturnsOnlyOwnTransactions()
        {
            // Arrange: 2 transactions — one for guestId, one for another user
            var guestId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    TransactionId   = Guid.NewGuid(),
                    TransactionDate = DateTime.UtcNow,
                    Reservation     = new Reservation { UserId = guestId }      // Guest's own
                },
                new Transaction
                {
                    TransactionId   = Guid.NewGuid(),
                    TransactionDate = DateTime.UtcNow,
                    Reservation     = new Reservation { UserId = otherUserId }  // Someone else's
                }
            };

            // FIX: .BuildMock()
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(transactions));

            // Act
            var result = await _sut.GetAllTransactionsAsync(guestId, "Guest", 1, 10);

            // Assert: only 1 result — the guest's own transaction
            result.TotalCount.Should().Be(1);
            result.Transactions.Should().AllSatisfy(t =>
            {
                transactions.First(tx => tx.TransactionId == t.TransactionId)
                            .Reservation!.UserId.Should().Be(guestId);
            });
        }

        [Fact]
        public async Task GetAllTransactionsAsync_SuperAdminRole_ReturnsAllTransactions()
        {
            // Arrange: 3 transactions from different users — SuperAdmin sees all
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    TransactionId   = Guid.NewGuid(),
                    TransactionDate = DateTime.UtcNow,
                    Reservation     = new Reservation { UserId = Guid.NewGuid() }
                },
                new Transaction
                {
                    TransactionId   = Guid.NewGuid(),
                    TransactionDate = DateTime.UtcNow,
                    Reservation     = new Reservation { UserId = Guid.NewGuid() }
                },
                new Transaction
                {
                    TransactionId   = Guid.NewGuid(),
                    TransactionDate = DateTime.UtcNow,
                    Reservation     = new Reservation { UserId = Guid.NewGuid() }
                }
            };

            // FIX: .BuildMock()
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(transactions));

            // Act
            var result = await _sut.GetAllTransactionsAsync(Guid.NewGuid(), "SuperAdmin", 1, 10);

            // Assert: all 3 visible to SuperAdmin
            result.TotalCount.Should().Be(3);
        }
    }
}