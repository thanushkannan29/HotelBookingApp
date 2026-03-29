using Xunit;
using Moq;
using MockQueryable.Moq;          // ← ADDED: gives .BuildMock() on any List<T>
using FluentAssertions;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Inventory;
using HotelBookingAppWebApi.Models.DTOs.Room;
using HotelBookingAppWebApi.Models.DTOs.RoomType;
using HotelBookingAppWebApi.Models.DTOs.UserDetails;
using HotelBookingAppWebApi.Exceptions;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Tests.Services
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // INVENTORY SERVICE TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for InventoryService.
    /// Key rules:
    ///   - AddInventory: Idempotent — skip dates that already have inventory records
    ///   - UpdateInventory: Cannot reduce TotalInventory below ReservedInventory
    ///
    /// FIX APPLIED:
    ///   All .AsQueryable() replaced with .AsQueryable().BuildMock()
    ///   so EF Core async methods (ToListAsync, AnyAsync, MaxAsync) work in tests.
    ///   Requires NuGet: MockQueryable.Moq version 7.0.0
    /// </summary>
    public class InventoryServiceTests
    {
        private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly InventoryService _sut;

        public InventoryServiceTests()
        {
            _sut = new InventoryService(
                _inventoryRepoMock.Object,
                _roomTypeRepoMock.Object,
                _userRepoMock.Object,
                _unitOfWorkMock.Object);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        }

        // ── HELPER ────────────────────────────────────────────────────────────
        private static IQueryable<T> ToMockQueryable<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        // ═══════════════════════════════════════════════════════════════════════
        // AddInventoryAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task AddInventoryAsync_NewDateRange_CreatesAllDates()
        {
            // Arrange: No existing inventory — all 3 dates must be created
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)); // +1, +2, +3

            var dto = new CreateInventoryDto
            {
                RoomTypeId = roomTypeId,
                StartDate = start,
                EndDate = end,
                TotalInventory = 5
            };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(new User { HotelId = hotelId });

            _roomTypeRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RoomType, bool>>>()))
                             .ReturnsAsync(new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId });

            // FIX: .BuildMock() — empty list, no existing inventory
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory>()));

            _inventoryRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomTypeInventory>()))
                              .ReturnsAsync((RoomTypeInventory inv) => inv);

            // Act
            await _sut.AddInventoryAsync(adminId, dto);

            // Assert: 3 records created, one per day in the range
            _inventoryRepoMock.Verify(r => r.AddAsync(It.IsAny<RoomTypeInventory>()), Times.Exactly(3));
        }

        [Fact]
        public async Task AddInventoryAsync_PartiallyExistingDates_OnlyCreatesNewDates()
        {
            // Arrange: Day +1 already exists in DB — only +2 and +3 should be created
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();
            var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(new User { HotelId = hotelId });

            _roomTypeRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RoomType, bool>>>()))
                             .ReturnsAsync(new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId });

            // FIX: .BuildMock() — day +1 already exists
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory>
                              {
                                  new RoomTypeInventory
                                  {
                                      RoomTypeId     = roomTypeId,
                                      Date           = start,     // Day +1 already in DB
                                      TotalInventory = 3
                                  }
                              }));

            _inventoryRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomTypeInventory>()))
                              .ReturnsAsync((RoomTypeInventory inv) => inv);

            var dto = new CreateInventoryDto
            {
                RoomTypeId = roomTypeId,
                StartDate = start,
                EndDate = end,
                TotalInventory = 5
            };

            // Act
            await _sut.AddInventoryAsync(adminId, dto);

            // Assert: Only 2 new records created (+2 and +3), day +1 skipped
            _inventoryRepoMock.Verify(r => r.AddAsync(It.IsAny<RoomTypeInventory>()), Times.Exactly(2));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UpdateInventoryAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateInventoryAsync_ReducingBelowReserved_ThrowsInsufficientInventoryException()
        {
            // Arrange: 3 rooms reserved, admin tries to set total to 2 — invalid
            var adminId = Guid.NewGuid();
            var inventoryId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            var existingInventory = new RoomTypeInventory
            {
                RoomTypeInventoryId = inventoryId,
                RoomTypeId = roomTypeId,
                TotalInventory = 5,
                ReservedInventory = 3,   // 3 rooms currently booked
                RoomType = new RoomType { HotelId = Guid.NewGuid() }
            };
            var dto = new UpdateInventoryDto
            {
                RoomTypeInventoryId = inventoryId,
                TotalInventory = 2   // 2 < 3 reserved → INVALID
            };

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory> { existingInventory }));

            // Act & Assert
            await Assert.ThrowsAsync<InsufficientInventoryException>(() =>
                _sut.UpdateInventoryAsync(adminId, dto));
        }

        [Fact]
        public async Task UpdateInventoryAsync_SettingToReservedCount_Succeeds()
        {
            // Arrange: Setting total exactly equal to reserved is valid (0 available, but not negative)
            var adminId = Guid.NewGuid();
            var inventoryId = Guid.NewGuid();

            var existingInventory = new RoomTypeInventory
            {
                RoomTypeInventoryId = inventoryId,
                TotalInventory = 5,
                ReservedInventory = 3,
                RoomType = new RoomType()
            };
            var dto = new UpdateInventoryDto
            {
                RoomTypeInventoryId = inventoryId,
                TotalInventory = 3   // Equal to reserved — valid
            };

            // FIX: .BuildMock()
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory> { existingInventory }));

            // Act (should NOT throw)
            await _sut.UpdateInventoryAsync(adminId, dto);

            // Assert
            existingInventory.TotalInventory.Should().Be(3);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ROOM TYPE SERVICE TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for RoomTypeService.
    /// Key rules:
    ///   - Rate date ranges cannot overlap for the same RoomType
    ///   - StartDate must be <= EndDate for rates
    /// </summary>
    public class RoomTypeServiceTests
    {
        private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomTypeRate>> _rateRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly RoomTypeService _sut;

        public RoomTypeServiceTests()
        {
            _sut = new RoomTypeService(
                _roomTypeRepoMock.Object,
                _rateRepoMock.Object,
                _userRepoMock.Object,
                _auditLogServiceMock.Object,
                _unitOfWorkMock.Object);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            _auditLogServiceMock.Setup(a => a.LogAsync(
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        // ── HELPER ────────────────────────────────────────────────────────────
        private static IQueryable<T> ToMockQueryable<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        // ═══════════════════════════════════════════════════════════════════════
        // AddRateAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task AddRateAsync_NonOverlappingDateRange_CreatesRate()
        {
            // Arrange: No existing rates — safe to add
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId }
                             }));

            // FIX: .BuildMock() — empty rates list
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<RoomTypeRate>()));

            _rateRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomTypeRate>()))
                         .ReturnsAsync((RoomTypeRate rt) => rt);

            var dto = new CreateRoomTypeRateDto
            {
                RoomTypeId = roomTypeId,
                StartDate = new DateOnly(2025, 12, 1),
                EndDate = new DateOnly(2025, 12, 31),
                Rate = 2500m
            };

            // Act
            await _sut.AddRateAsync(adminId, dto);

            // Assert
            _rateRepoMock.Verify(r => r.AddAsync(It.Is<RoomTypeRate>(rt =>
                rt.Rate == 2500m &&
                rt.RoomTypeId == roomTypeId)), Times.Once);
        }

        [Fact]
        public async Task AddRateAsync_OverlappingDateRange_ThrowsConflictException()
        {
            // Arrange: Existing rate covers Dec 1-31, new rate overlaps (Dec 15 - Jan 15)
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId }
                             }));

            // FIX: .BuildMock() — existing rate that overlaps with the new one
            _rateRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<RoomTypeRate>
                         {
                             new RoomTypeRate
                             {
                                 RoomTypeId = roomTypeId,
                                 StartDate  = new DateOnly(2025, 12, 1),
                                 EndDate    = new DateOnly(2025, 12, 31)
                             }
                         }));

            var dto = new CreateRoomTypeRateDto
            {
                RoomTypeId = roomTypeId,
                StartDate = new DateOnly(2025, 12, 15),  // Overlaps with existing!
                EndDate = new DateOnly(2026, 1, 15),
                Rate = 3000m
            };

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _sut.AddRateAsync(adminId, dto));
        }

        [Fact]
        public async Task AddRateAsync_StartDateAfterEndDate_ThrowsValidationException()
        {
            // Arrange: StartDate is AFTER EndDate — invalid
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId }
                             }));

            var dto = new CreateRoomTypeRateDto
            {
                RoomTypeId = roomTypeId,
                StartDate = new DateOnly(2025, 12, 31),  // Start AFTER end!
                EndDate = new DateOnly(2025, 12, 1),
                Rate = 1000m
            };

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _sut.AddRateAsync(adminId, dto));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // AddRoomTypeAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task AddRoomTypeAsync_ValidAdmin_CreatesRoomType()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            _roomTypeRepoMock.Setup(r => r.AddAsync(It.IsAny<RoomType>()))
                             .ReturnsAsync((RoomType rt) => rt);

            var dto = new CreateRoomTypeDto
            {
                Name = "Deluxe Suite",
                MaxOccupancy = 2,
                Amenities = "WiFi,AC,TV",
                Description = "Spacious suite"
            };

            // Act
            await _sut.AddRoomTypeAsync(adminId, dto);

            // Assert: RoomType created with correct name and linked hotel
            _roomTypeRepoMock.Verify(r => r.AddAsync(It.Is<RoomType>(rt =>
                rt.Name == "Deluxe Suite" &&
                rt.HotelId == hotelId)), Times.Once);
        }

        [Fact]
        public async Task AddRoomTypeAsync_AdminWithNoHotel_ThrowsUnAuthorizedException()
        {
            // Arrange: Admin has no hotel linked (HotelId = null)
            var adminId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = null });  // No hotel!

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.AddRoomTypeAsync(adminId, new CreateRoomTypeDto { Name = "Standard" }));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // ROOM SERVICE TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for RoomService.
    /// Key rules:
    ///   - Cannot add more rooms than inventory cap (MaxInventory)
    ///   - Room number must be unique within the hotel
    ///   - RoomType must belong to admin's hotel
    /// </summary>
    public class RoomServiceTests
    {
        private readonly Mock<IRepository<Guid, Room>> _roomRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomTypeInventory>> _inventoryRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly RoomService _sut;

        public RoomServiceTests()
        {
            _sut = new RoomService(
                _roomRepoMock.Object,
                _roomTypeRepoMock.Object,
                _inventoryRepoMock.Object,
                _userRepoMock.Object,
                _auditLogServiceMock.Object,
                _unitOfWorkMock.Object);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            _auditLogServiceMock.Setup(a => a.LogAsync(
                It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Guid?>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        // ── HELPER ────────────────────────────────────────────────────────────
        private static IQueryable<T> ToMockQueryable<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        // ═══════════════════════════════════════════════════════════════════════
        // AddRoomAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task AddRoomAsync_DuplicateRoomNumber_ThrowsConflictException()
        {
            // Arrange: Room "101" already exists in this hotel
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId }
                             }));

            // FIX: .BuildMock() — room 101 already exists
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room { RoomNumber = "101", HotelId = hotelId, RoomTypeId = roomTypeId }
                         }));

            var dto = new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId };

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _sut.AddRoomAsync(adminId, dto));
        }

        [Fact]
        public async Task AddRoomAsync_ExceedingInventoryCap_ThrowsConflictException()
        {
            // Arrange: Inventory cap is 2, but 2 rooms already exist — cannot add a 3rd
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId }
                             }));

            // FIX: .BuildMock() — 2 existing rooms with different numbers
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>
                         {
                             new Room { RoomNumber = "101", HotelId = hotelId, RoomTypeId = roomTypeId },
                             new Room { RoomNumber = "102", HotelId = hotelId, RoomTypeId = roomTypeId }
                         }));

            // FIX: .BuildMock() — inventory cap is 2
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory>
                              {
                                  new RoomTypeInventory { RoomTypeId = roomTypeId, TotalInventory = 2 }
                              }));

            var dto = new CreateRoomDto { RoomNumber = "103", Floor = 1, RoomTypeId = roomTypeId };

            // Act & Assert: 2 rooms already == cap of 2 → cannot add 103
            await Assert.ThrowsAsync<ConflictException>(() => _sut.AddRoomAsync(adminId, dto));
        }

        [Fact]
        public async Task AddRoomAsync_NoInventoryDefined_ThrowsNotFoundException()
        {
            // Arrange: No inventory configured at all for this room type
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var roomTypeId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetAsync(adminId))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });

            // FIX: .BuildMock()
            _roomTypeRepoMock.Setup(r => r.GetQueryable())
                             .Returns(ToMockQueryable(new List<RoomType>
                             {
                                 new RoomType { RoomTypeId = roomTypeId, HotelId = hotelId }
                             }));

            // FIX: .BuildMock() — no rooms yet
            _roomRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<Room>()));

            // FIX: .BuildMock() — no inventory configured
            _inventoryRepoMock.Setup(r => r.GetQueryable())
                              .Returns(ToMockQueryable(new List<RoomTypeInventory>()));

            var dto = new CreateRoomDto { RoomNumber = "101", Floor = 1, RoomTypeId = roomTypeId };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddRoomAsync(adminId, dto));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // USER SERVICE TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for UserService.
    /// Key rules:
    ///   - UpdateProfile is a partial update — only non-null/non-empty fields change
    ///   - GetProfile requires UserDetails to exist (one-to-one relationship)
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _sut = new UserService(
                _userRepoMock.Object,
                _reservationRepoMock.Object,
                _unitOfWorkMock.Object);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        }

        // ── HELPER ────────────────────────────────────────────────────────────
        private static IQueryable<T> ToMockQueryable<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        // ═══════════════════════════════════════════════════════════════════════
        // GetProfileAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                Email = "test@test.com",
                Role = UserRole.Guest,
                UserDetails = new UserProfileDetails
                {
                    Name = "Test User",
                    PhoneNumber = "9876543210",
                    Address = "123 Main St",
                    State = "Maharashtra",
                    City = "Mumbai",
                    Pincode = "400001",
                    CreatedAt = DateTime.UtcNow
                }
            };

            // FIX: .BuildMock()
            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<User> { user }));

            // Act
            var result = await _sut.GetProfileAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("test@test.com");
            result.Name.Should().Be("Test User");
            result.Role.Should().Be("Guest");
        }

        [Fact]
        public async Task GetProfileAsync_NonExistentUser_ThrowsNotFoundException()
        {
            // Arrange: Empty list — user not found
            // FIX: .BuildMock()
            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<User>()));

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.GetProfileAsync(Guid.NewGuid()));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UpdateProfileAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateProfileAsync_OnlyUpdatesNonNullFields()
        {
            // Arrange: Only Name and PhoneNumber provided — Address/State/City must stay unchanged
            var userId = Guid.NewGuid();
            var details = new UserProfileDetails
            {
                Name = "Old Name",
                PhoneNumber = "1111111111",
                Address = "Old Address",  // Must NOT change
                State = "Old State",    // Must NOT change
                City = "Old City",     // Must NOT change
                Pincode = "000000",
                CreatedAt = DateTime.UtcNow
            };
            var user = new User
            {
                UserId = userId,
                Email = "test@test.com",
                Role = UserRole.Guest,
                UserDetails = details
            };

            // FIX: .BuildMock()
            _userRepoMock.Setup(r => r.GetQueryable())
                         .Returns(ToMockQueryable(new List<User> { user }));

            var dto = new UpdateUserProfileDto
            {
                Name = "New Name",          // Update
                PhoneNumber = "9999999999",         // Update
                Address = null,                 // Keep original (null = no change)
                State = null,                 // Keep original
                City = null                  // Keep original
            };

            // Act
            await _sut.UpdateProfileAsync(userId, dto);

            // Assert: Updated fields changed, null fields unchanged
            details.Name.Should().Be("New Name");
            details.PhoneNumber.Should().Be("9999999999");
            details.Address.Should().Be("Old Address");  // Unchanged
            details.State.Should().Be("Old State");      // Unchanged
            details.City.Should().Be("Old City");        // Unchanged
        }
    }
}