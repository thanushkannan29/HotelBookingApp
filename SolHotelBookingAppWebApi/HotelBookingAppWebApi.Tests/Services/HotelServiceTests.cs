using Xunit;
using Moq;
using MockQueryable.Moq;          // ← ADDED: gives .BuildMock() on any List<T>
using FluentAssertions;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Admin;
using HotelBookingAppWebApi.Models.DTOs.Hotel.Public;
using HotelBookingAppWebApi.Exceptions;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Tests.Services
{
    /// <summary>
    /// Tests for HotelService.
    /// Key rules:
    ///   - Admin can only update their own hotel (HotelId must match)
    ///   - Cannot activate a hotel blocked by SuperAdmin
    ///   - BlockHotel sets IsActive=false AND IsBlockedBySuperAdmin=true
    ///   - UnblockHotel only removes the block, does NOT reactivate
    ///
    /// FIX APPLIED:
    ///   All .AsQueryable() calls replaced with .AsQueryable().BuildMock()
    ///   so EF Core async methods (ToListAsync, CountAsync, AnyAsync) work in tests.
    ///   Requires NuGet package: MockQueryable.Moq version 7.0.0
    /// </summary>
    public class HotelServiceTests
    {
        private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IRepository<Guid, RoomType>> _roomTypeRepoMock = new();
        private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly HotelService _sut;

        public HotelServiceTests()
        {
            _sut = new HotelService(
                _hotelRepoMock.Object,
                _userRepoMock.Object,
                _roomTypeRepoMock.Object,
                _transactionRepoMock.Object,
                _reservationRepoMock.Object,
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

        /// <summary>
        /// Wraps a List into a BuildMock() queryable so EF Core async methods work.
        /// Use this for every repo.GetQueryable() setup in this file.
        /// </summary>
        private static IQueryable<T> ToMockQueryable<T>(IEnumerable<T> list) where T : class
            => list.AsQueryable().BuildMock();

        // ═══════════════════════════════════════════════════════════════════════
        // ToggleHotelStatusAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task ToggleHotelStatusAsync_ActivateBlockedHotel_ThrowsValidationException()
        {
            // Arrange: Hotel is blocked by SuperAdmin — admin cannot reactivate it
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            var admin = new User { UserId = adminId, HotelId = hotelId };
            var hotel = new Hotel
            {
                HotelId = hotelId,
                IsActive = false,
                IsBlockedBySuperAdmin = true  // Blocked!
            };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(admin);
            _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);

            // Act & Assert: Admin tries to activate but it's blocked
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                _sut.ToggleHotelStatusAsync(adminId, true));

            ex.Message.Should().Contain("blocked");
        }

        [Fact]
        public async Task ToggleHotelStatusAsync_DeactivateHotel_Succeeds()
        {
            // Arrange: Hotel is active and not blocked — admin can deactivate
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            var admin = new User { UserId = adminId, HotelId = hotelId };
            var hotel = new Hotel { HotelId = hotelId, IsActive = true, IsBlockedBySuperAdmin = false };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(admin);
            _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);

            // Act
            await _sut.ToggleHotelStatusAsync(adminId, false);

            // Assert
            hotel.IsActive.Should().BeFalse();
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task ToggleHotelStatusAsync_ActivateUnblockedHotel_Succeeds()
        {
            // Arrange: Hotel is not blocked — admin can freely activate
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            var admin = new User { UserId = adminId, HotelId = hotelId };
            var hotel = new Hotel { HotelId = hotelId, IsActive = false, IsBlockedBySuperAdmin = false };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(admin);
            _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);

            // Act
            await _sut.ToggleHotelStatusAsync(adminId, true);

            // Assert
            hotel.IsActive.Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BlockHotelAsync / UnblockHotelAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task BlockHotelAsync_SetsBlockedFlagAndDeactivates()
        {
            // Arrange
            var hotelId = Guid.NewGuid();
            var hotel = new Hotel { HotelId = hotelId, IsActive = true, IsBlockedBySuperAdmin = false };

            _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);

            // Act
            await _sut.BlockHotelAsync(hotelId);

            // Assert: Both flags must be set when blocking
            hotel.IsBlockedBySuperAdmin.Should().BeTrue();
            hotel.IsActive.Should().BeFalse();  // Forced inactive when blocked
        }

        [Fact]
        public async Task BlockHotelAsync_NonExistentHotel_ThrowsNotFoundException()
        {
            // Arrange: GetAsync returns null — hotel does not exist
            _hotelRepoMock.Setup(r => r.GetAsync(It.IsAny<Guid>()))
                          .ReturnsAsync((Hotel?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.BlockHotelAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task UnblockHotelAsync_RemovesBlockFlagOnly()
        {
            // Arrange
            var hotelId = Guid.NewGuid();
            var hotel = new Hotel { HotelId = hotelId, IsActive = false, IsBlockedBySuperAdmin = true };

            _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);

            // Act
            await _sut.UnblockHotelAsync(hotelId);

            // Assert: Block flag removed, but IsActive stays false
            // Admin must manually call ToggleHotelStatus to reactivate after unblocking
            hotel.IsBlockedBySuperAdmin.Should().BeFalse();
            hotel.IsActive.Should().BeFalse();  // NOT automatically reactivated
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UpdateHotelAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateHotelAsync_ValidAdmin_UpdatesAllHotelFields()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            var admin = new User { UserId = adminId, HotelId = hotelId };
            var hotel = new Hotel
            {
                HotelId = hotelId,
                Name = "Old Name",
                City = "Old City",
                Address = "Old Address",
                Description = "Old Desc",
                ContactNumber = "1111111111"
            };
            var dto = new UpdateHotelDto
            {
                Name = "New Grand Hotel",
                City = "Mumbai",
                Address = "456 New St",
                Description = "Updated description",
                ContactNumber = "9999999999",
                ImageUrl = "https://example.com/image.jpg"
            };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(admin);
            _hotelRepoMock.Setup(r => r.GetAsync(hotelId)).ReturnsAsync(hotel);

            // Act
            await _sut.UpdateHotelAsync(adminId, dto);

            // Assert: All fields must be updated
            hotel.Name.Should().Be("New Grand Hotel");
            hotel.City.Should().Be("Mumbai");
            hotel.Address.Should().Be("456 New St");
            hotel.ContactNumber.Should().Be("9999999999");
            hotel.ImageUrl.Should().Be("https://example.com/image.jpg");
        }

        [Fact]
        public async Task UpdateHotelAsync_AdminWithNoHotel_ThrowsUnAuthorizedException()
        {
            // Arrange: Admin has no HotelId linked to their account
            var adminId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = null });  // No hotel!

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.UpdateHotelAsync(adminId, new UpdateHotelDto { Name = "Test" }));
        }

        [Fact]
        public async Task UpdateHotelAsync_WritesAuditLog()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(new User { UserId = adminId, HotelId = hotelId });
            _hotelRepoMock.Setup(r => r.GetAsync(hotelId))
                          .ReturnsAsync(new Hotel
                          {
                              HotelId = hotelId,
                              Name = "Hotel A",
                              City = "Delhi",
                              Address = "X",
                              Description = "Y",
                              ContactNumber = "1234567890"
                          });

            // Act
            await _sut.UpdateHotelAsync(adminId, new UpdateHotelDto
            {
                Name = "Hotel B",
                City = "Mumbai",
                Address = "Y",
                Description = "Z",
                ContactNumber = "0987654321"
            });

            // Assert: Audit log must be written with "HotelUpdated" action
            _auditLogServiceMock.Verify(a => a.LogAsync(
                adminId,
                "HotelUpdated",
                "Hotel",
                hotelId,
                It.IsAny<string>()), Times.Once);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // GetTopHotelsAsync Tests
        // FIX: Use .BuildMock() so ToListAsync() works inside the service
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetTopHotelsAsync_ExcludesInactiveHotels()
        {
            // Arrange
            var hotels = new List<Hotel>
            {
                new Hotel
                {
                    HotelId               = Guid.NewGuid(),
                    Name                  = "Active Hotel",
                    City                  = "Delhi",
                    IsActive              = true,
                    IsBlockedBySuperAdmin = false,
                    Reviews               = new List<Review>(),
                    RoomTypes             = new List<RoomType>()
                },
                new Hotel
                {
                    HotelId               = Guid.NewGuid(),
                    Name                  = "Inactive Hotel",
                    City                  = "Mumbai",
                    IsActive              = false,   // Should be excluded
                    IsBlockedBySuperAdmin = false,
                    Reviews               = new List<Review>(),
                    RoomTypes             = new List<RoomType>()
                }
            };

            // FIX: .BuildMock() instead of .AsQueryable()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(hotels));

            // Act
            var result = await _sut.GetTopHotelsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Active Hotel");
        }

        [Fact]
        public async Task GetTopHotelsAsync_ExcludesBlockedHotels()
        {
            // Arrange
            var hotels = new List<Hotel>
            {
                new Hotel
                {
                    HotelId               = Guid.NewGuid(),
                    Name                  = "Good Hotel",
                    IsActive              = true,
                    IsBlockedBySuperAdmin = false,
                    Reviews               = new List<Review>(),
                    RoomTypes             = new List<RoomType>()
                },
                new Hotel
                {
                    HotelId               = Guid.NewGuid(),
                    Name                  = "Blocked Hotel",
                    IsActive              = false,
                    IsBlockedBySuperAdmin = true,  // Blocked by SuperAdmin — should be excluded
                    Reviews               = new List<Review>(),
                    RoomTypes             = new List<RoomType>()
                }
            };

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(hotels));

            // Act
            var result = await _sut.GetTopHotelsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Good Hotel");
        }

        [Fact]
        public async Task GetTopHotelsAsync_ReturnsMaximum10Hotels()
        {
            // Arrange: 15 active hotels — service should Take(10)
            var hotels = Enumerable.Range(1, 15).Select(i => new Hotel
            {
                HotelId = Guid.NewGuid(),
                Name = $"Hotel {i}",
                City = "Chennai",
                IsActive = true,
                IsBlockedBySuperAdmin = false,
                Reviews = new List<Review> { new Review { Rating = (decimal)(i % 5 + 1) } },
                RoomTypes = new List<RoomType>()
            }).ToList();

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(hotels));

            // Act
            var result = await _sut.GetTopHotelsAsync();

            // Assert: Max 10 returned
            result.Should().HaveCount(10);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SearchHotelsAsync Tests
        // FIX: Use .BuildMock() so CountAsync() and ToListAsync() work
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task SearchHotelsAsync_NoHotelsInCity_ThrowsNotFoundException()
        {
            // Arrange: Only a Delhi hotel exists, searching for Pune
            var hotels = new List<Hotel>
            {
                new Hotel
                {
                    HotelId               = Guid.NewGuid(),
                    Name                  = "Delhi Hotel",
                    City                  = "Delhi",
                    IsActive              = true,
                    IsBlockedBySuperAdmin = false,
                    Reviews               = new List<Review>(),
                    RoomTypes             = new List<RoomType>()
                }
            };

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(hotels));

            var request = new SearchHotelRequestDto
            {
                City = "Pune",   // No hotels in this city
                CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                PageNumber = 1,
                PageSize = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.SearchHotelsAsync(request));
        }

        [Fact]
        public async Task SearchHotelsAsync_CitySearchIsCaseInsensitive()
        {
            // Arrange: City stored as "Mumbai", searched as "mumbai" (lowercase)
            var hotels = new List<Hotel>
            {
                new Hotel
                {
                    HotelId               = Guid.NewGuid(),
                    Name                  = "Sea View Hotel",
                    City                  = "Mumbai",
                    IsActive              = true,
                    IsBlockedBySuperAdmin = false,
                    Reviews               = new List<Review>(),
                    RoomTypes             = new List<RoomType>()
                }
            };

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(hotels));

            var request = new SearchHotelRequestDto
            {
                City = "mumbai",  // Lowercase — should still match "Mumbai"
                CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _sut.SearchHotelsAsync(request);

            // Assert
            result.Hotels.Should().HaveCount(1);
            result.Hotels.First().Name.Should().Be("Sea View Hotel");
        }
    }
}