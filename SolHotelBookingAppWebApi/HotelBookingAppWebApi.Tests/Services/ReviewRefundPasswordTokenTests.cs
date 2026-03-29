using Xunit;
using Moq;
using MockQueryable.Moq;          // ← ADDED: gives .BuildMock() on any List<T>
using FluentAssertions;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Review;
using HotelBookingAppWebApi.Models.DTOs.RefundRequest;
using HotelBookingAppWebApi.Exceptions;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Tests.Services
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // REVIEW SERVICE TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for ReviewService.
    /// Key rules:
    ///   - Guest MUST have a Completed reservation to review (prevents fake reviews)
    ///   - One review per guest per hotel
    ///   - Only the review author can update/delete their review
    ///
    /// FIX APPLIED:
    ///   All .AsQueryable() replaced with .AsQueryable().BuildMock()
    ///   so EF Core async methods (AnyAsync, ToListAsync) work in tests.
    ///   Requires NuGet: MockQueryable.Moq version 7.0.0
    /// </summary>
    public class ReviewServiceTests
    {
        private readonly Mock<IRepository<Guid, Review>> _reviewRepoMock = new();
        private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ReviewService _sut;

        public ReviewServiceTests()
        {
            _sut = new ReviewService(
                _reviewRepoMock.Object,
                _hotelRepoMock.Object,
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
        // AddReviewAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task AddReviewAsync_WithCompletedStay_CreatesReview()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var dto = new CreateReviewDto { HotelId = hotelId, Rating = 4.5m, Comment = "Great stay!" };

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(new List<Hotel> { new Hotel { HotelId = hotelId } }));

            // FIX: .BuildMock() — guest has a completed stay
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation>
                                 {
                                     new Reservation
                                     {
                                         UserId  = userId,
                                         HotelId = hotelId,
                                         Status  = ReservationStatus.Completed
                                     }
                                 }));

            // FIX: .BuildMock() — no existing review
            _reviewRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<Review>()));

            _reviewRepoMock.Setup(r => r.AddAsync(It.IsAny<Review>()))
                           .ReturnsAsync((Review rv) => rv);

            // Act
            var result = await _sut.AddReviewAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            result.HotelId.Should().Be(hotelId);
            result.Rating.Should().Be(4.5m);
            _reviewRepoMock.Verify(r => r.AddAsync(It.IsAny<Review>()), Times.Once);
        }

        [Fact]
        public async Task AddReviewAsync_WithoutCompletedStay_ThrowsReviewException()
        {
            // Arrange: guest has NO reservation at all
            var userId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var dto = new CreateReviewDto { HotelId = hotelId, Rating = 5m, Comment = "Fake review attempt" };

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(new List<Hotel> { new Hotel { HotelId = hotelId } }));

            // FIX: .BuildMock() — empty list, no reservation
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation>()));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ReviewException>(() =>
                _sut.AddReviewAsync(userId, dto));
            ex.Message.Should().Contain("completing a stay");
        }

        [Fact]
        public async Task AddReviewAsync_WithPendingReservation_ThrowsReviewException()
        {
            // Arrange: guest has a Pending reservation — not good enough to review
            var userId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var dto = new CreateReviewDto { HotelId = hotelId, Rating = 3m, Comment = "Too early to review" };

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(new List<Hotel> { new Hotel { HotelId = hotelId } }));

            // FIX: .BuildMock() — Pending ≠ Completed
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation>
                                 {
                                     new Reservation
                                     {
                                         UserId  = userId,
                                         HotelId = hotelId,
                                         Status  = ReservationStatus.Pending  // Not Completed
                                     }
                                 }));

            // Act & Assert
            await Assert.ThrowsAsync<ReviewException>(() => _sut.AddReviewAsync(userId, dto));
        }

        [Fact]
        public async Task AddReviewAsync_AlreadyReviewed_ThrowsReviewException()
        {
            // Arrange: guest already left a review for this hotel
            var userId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var dto = new CreateReviewDto { HotelId = hotelId, Rating = 5m, Comment = "Second review attempt" };

            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(new List<Hotel> { new Hotel { HotelId = hotelId } }));

            // FIX: .BuildMock() — has completed stay
            _reservationRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Reservation>
                                 {
                                     new Reservation
                                     {
                                         UserId  = userId,
                                         HotelId = hotelId,
                                         Status  = ReservationStatus.Completed
                                     }
                                 }));

            // FIX: .BuildMock() — existing review for this hotel
            _reviewRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<Review>
                           {
                               new Review { UserId = userId, HotelId = hotelId }
                           }));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ReviewException>(() => _sut.AddReviewAsync(userId, dto));
            ex.Message.Should().Contain("already reviewed");
        }

        [Fact]
        public async Task AddReviewAsync_NonExistentHotel_ThrowsNotFoundException()
        {
            // Arrange: empty hotel list
            // FIX: .BuildMock()
            _hotelRepoMock.Setup(r => r.GetQueryable())
                          .Returns(ToMockQueryable(new List<Hotel>()));

            var dto = new CreateReviewDto { HotelId = Guid.NewGuid(), Rating = 4m, Comment = "Test" };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _sut.AddReviewAsync(Guid.NewGuid(), dto));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UpdateReviewAsync Tests — use GetAsync, no BuildMock needed
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateReviewAsync_ByOwner_UpdatesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var reviewId = Guid.NewGuid();
            var existing = new Review
            {
                ReviewId = reviewId,
                UserId = userId,
                HotelId = Guid.NewGuid(),
                Rating = 3m,
                Comment = "Old comment"
            };
            var dto = new UpdateReviewDto { Rating = 5m, Comment = "Updated comment" };

            _reviewRepoMock.Setup(r => r.GetAsync(reviewId)).ReturnsAsync(existing);
            _reviewRepoMock.Setup(r => r.UpdateAsync(reviewId, existing)).ReturnsAsync(existing);

            // Act
            var result = await _sut.UpdateReviewAsync(userId, reviewId, dto);

            // Assert
            result.Rating.Should().Be(5m);
            existing.Comment.Should().Be("Updated comment");
        }

        [Fact]
        public async Task UpdateReviewAsync_ByNonOwner_ThrowsReviewException()
        {
            // Arrange: attackerId tries to edit reviewOwnerId's review
            var reviewOwnerId = Guid.NewGuid();
            var attackerId = Guid.NewGuid();
            var reviewId = Guid.NewGuid();
            var review = new Review { ReviewId = reviewId, UserId = reviewOwnerId, Rating = 4m, Comment = "Original" };

            _reviewRepoMock.Setup(r => r.GetAsync(reviewId)).ReturnsAsync(review);

            // Act & Assert
            await Assert.ThrowsAsync<ReviewException>(() =>
                _sut.UpdateReviewAsync(attackerId, reviewId, new UpdateReviewDto { Rating = 1m }));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DeleteReviewAsync Tests — use GetAsync, no BuildMock needed
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteReviewAsync_ByOwner_DeletesSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var reviewId = Guid.NewGuid();
            var review = new Review { ReviewId = reviewId, UserId = userId };

            _reviewRepoMock.Setup(r => r.GetAsync(reviewId)).ReturnsAsync(review);
            _reviewRepoMock.Setup(r => r.DeleteAsync(reviewId)).ReturnsAsync(review);

            // Act
            var result = await _sut.DeleteReviewAsync(userId, reviewId);

            // Assert
            result.Should().BeTrue();
            _reviewRepoMock.Verify(r => r.DeleteAsync(reviewId), Times.Once);
        }

        [Fact]
        public async Task DeleteReviewAsync_ByNonOwner_ThrowsReviewException()
        {
            // Arrange
            var ownerUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var reviewId = Guid.NewGuid();
            var review = new Review { ReviewId = reviewId, UserId = ownerUserId };

            _reviewRepoMock.Setup(r => r.GetAsync(reviewId)).ReturnsAsync(review);

            // Act & Assert
            await Assert.ThrowsAsync<ReviewException>(() =>
                _sut.DeleteReviewAsync(otherUserId, reviewId));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // REFUND REQUEST SERVICE TESTS
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for RefundRequestService.
    /// Key rules:
    ///   - CreateRefundRequestAsync is idempotent (called twice → only one request created)
    ///   - ApproveRefundAsync: must be admin's hotel, must be Pending
    ///   - RejectRefundAsync: must be admin's hotel, must be Pending
    /// </summary>
    public class RefundRequestServiceTests
    {
        private readonly Mock<IRepository<Guid, RefundRequest>> _refundRepoMock = new();
        private readonly Mock<IRepository<Guid, Transaction>> _transactionRepoMock = new();
        private readonly Mock<IRepository<Guid, Reservation>> _reservationRepoMock = new();
        private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
        private readonly Mock<IAuditLogService> _auditLogServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly RefundRequestService _sut;

        public RefundRequestServiceTests()
        {
            _sut = new RefundRequestService(
                _refundRepoMock.Object,
                _transactionRepoMock.Object,
                _reservationRepoMock.Object,
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
        // CreateRefundRequestAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateRefundRequestAsync_FirstTime_CreatesRequest()
        {
            // Arrange: No existing pending request
            var reservationId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // FIX: .BuildMock() — empty list, no existing request
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<RefundRequest>()));

            _refundRepoMock.Setup(r => r.AddAsync(It.IsAny<RefundRequest>()))
                           .ReturnsAsync((RefundRequest req) => req);

            // Act
            await _sut.CreateRefundRequestAsync(reservationId, userId, "trip cancelled");

            // Assert: one request created with Pending status
            _refundRepoMock.Verify(r => r.AddAsync(It.Is<RefundRequest>(req =>
                req.ReservationId == reservationId &&
                req.UserId == userId &&
                req.Status == RefundRequestStatus.Pending)), Times.Once);
        }

        [Fact]
        public async Task CreateRefundRequestAsync_CalledTwice_IsIdempotent()
        {
            // Arrange: pending request already exists
            var reservationId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // FIX: .BuildMock() — existing pending request
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<RefundRequest>
                           {
                               new RefundRequest
                               {
                                   ReservationId = reservationId,
                                   Status        = RefundRequestStatus.Pending
                               }
                           }));

            // Act
            await _sut.CreateRefundRequestAsync(reservationId, userId, "again");

            // Assert: NO second request added — idempotent
            _refundRepoMock.Verify(r => r.AddAsync(It.IsAny<RefundRequest>()), Times.Never);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ApproveRefundAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task ApproveRefundAsync_WithValidAdminAndPendingRequest_ApprovesAndRefunds()
        {
            // Arrange
            var refundRequestId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var successTransaction = new Transaction { Status = PaymentStatus.Success, Amount = 3000m };

            var reservation = new Reservation
            {
                HotelId = hotelId,
                ReservationCode = "RES-TEST001",
                Transactions = new List<Transaction> { successTransaction }
            };
            var refundRequest = new RefundRequest
            {
                RefundRequestId = refundRequestId,
                Status = RefundRequestStatus.Pending,
                Reservation = reservation,
                User = new User { Name = "Guest" }
            };
            var admin = new User { UserId = adminId, HotelId = hotelId };

            // FIX: .BuildMock()
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<RefundRequest> { refundRequest }));

            _userRepoMock.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

            // Act
            var result = await _sut.ApproveRefundAsync(refundRequestId, adminId, "Approved, valid cancellation");

            // Assert
            refundRequest.Status.Should().Be(RefundRequestStatus.Approved);
            successTransaction.Status.Should().Be(PaymentStatus.Refunded);  // Transaction marked refunded
            refundRequest.AdminResponse.Should().Be("Approved, valid cancellation");
            result.RefundAmount.Should().Be(3000m);
        }

        [Fact]
        public async Task ApproveRefundAsync_WrongAdmin_ThrowsUnAuthorizedException()
        {
            // Arrange: admin's hotel ≠ refund's hotel
            var refundRequestId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var adminHotelId = Guid.NewGuid();
            var otherHotelId = Guid.NewGuid();

            var refundRequest = new RefundRequest
            {
                RefundRequestId = refundRequestId,
                Status = RefundRequestStatus.Pending,
                Reservation = new Reservation
                {
                    HotelId = otherHotelId,    // Different hotel
                    Transactions = new List<Transaction>()
                },
                User = new User()
            };
            var admin = new User { UserId = adminId, HotelId = adminHotelId }; // Admin's hotel is different

            // FIX: .BuildMock()
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<RefundRequest> { refundRequest }));

            _userRepoMock.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(() =>
                _sut.ApproveRefundAsync(refundRequestId, adminId, "Approved"));
        }

        [Fact]
        public async Task ApproveRefundAsync_AlreadyProcessedRequest_ThrowsValidationException()
        {
            // Arrange: request is already Approved — cannot approve again
            var refundRequestId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();

            var refundRequest = new RefundRequest
            {
                RefundRequestId = refundRequestId,
                Status = RefundRequestStatus.Approved,  // Already processed!
                Reservation = new Reservation { HotelId = hotelId, Transactions = new List<Transaction>() },
                User = new User()
            };
            var admin = new User { UserId = adminId, HotelId = hotelId };

            // FIX: .BuildMock()
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<RefundRequest> { refundRequest }));

            _userRepoMock.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() =>
                _sut.ApproveRefundAsync(refundRequestId, adminId, "Try again"));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RejectRefundAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task RejectRefundAsync_WithValidAdminAndPendingRequest_RejectsWithNoFinancialChange()
        {
            // Arrange
            var refundRequestId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var reservationId = Guid.NewGuid();

            var refundRequest = new RefundRequest
            {
                RefundRequestId = refundRequestId,
                ReservationId = reservationId,
                Status = RefundRequestStatus.Pending,
                Reservation = new Reservation { HotelId = hotelId, ReservationCode = "RES-REJ001" },
                User = new User()
            };
            var admin = new User { UserId = adminId, HotelId = hotelId };

            // FIX: .BuildMock()
            _refundRepoMock.Setup(r => r.GetQueryable())
                           .Returns(ToMockQueryable(new List<RefundRequest> { refundRequest }));

            _userRepoMock.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);

            // FIX: .BuildMock() — empty transactions (no payment to refund on reject)
            _transactionRepoMock.Setup(r => r.GetQueryable())
                                 .Returns(ToMockQueryable(new List<Transaction>()));

            // Act
            var result = await _sut.RejectRefundAsync(refundRequestId, adminId, "No valid reason for refund");

            // Assert
            refundRequest.Status.Should().Be(RefundRequestStatus.Rejected);
            refundRequest.AdminResponse.Should().Be("No valid reason for refund");
            // No transaction status changed — rejection = no money back
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // PASSWORD SERVICE TESTS
    // Pure unit tests — no mocks or BuildMock needed (no EF Core calls)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for PasswordService.
    /// Pure cryptographic service — no external dependencies.
    /// Tests: hash generation, salt uniqueness, deterministic verification.
    /// </summary>
    public class PasswordServiceTests
    {
        private readonly PasswordService _sut = new PasswordService();

        [Fact]
        public void HashPassword_Registration_ReturnsHashAndSalt()
        {
            // Act: registration mode — no existing salt
            var hash = _sut.HashPassword("mypassword123", null, out var salt);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            salt.Should().NotBeNullOrEmpty();
            hash.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void HashPassword_WithSameSalt_ProducesSameHash()
        {
            // Arrange: register → get hash + salt
            var password = "SecurePassword!";
            var firstHash = _sut.HashPassword(password, null, out var generatedSalt);

            // Act: login → re-hash with stored salt
            var verificationHash = _sut.HashPassword(password, generatedSalt, out _);

            // Assert: must match for login to succeed
            verificationHash.Should().Equal(firstHash);
        }

        [Fact]
        public void HashPassword_WithDifferentSalt_ProducesDifferentHash()
        {
            // Arrange: generate two different salts
            var password = "SamePassword";
            _sut.HashPassword(password, null, out var salt1);
            _sut.HashPassword(password, null, out var salt2);

            // Act
            var hash1 = _sut.HashPassword(password, salt1, out _);
            var hash2 = _sut.HashPassword(password, salt2, out _);

            // Assert: same password + different salt = different hash (rainbow table protection)
            hash1.Should().NotEqual(hash2);
        }

        [Fact]
        public void HashPassword_DifferentPasswords_ProduceDifferentHashes()
        {
            // Arrange
            _sut.HashPassword("password1", null, out var salt);

            // Act
            var hash1 = _sut.HashPassword("password1", salt, out _);
            var hash2 = _sut.HashPassword("password2", salt, out _);

            // Assert
            hash1.Should().NotEqual(hash2);
        }

        [Fact]
        public void HashPassword_LoginMode_DoesNotOutputNewSalt()
        {
            // Arrange
            _sut.HashPassword("password", null, out var salt);

            // Act: login mode — existing salt provided
            _sut.HashPassword("password", salt, out var newSalt);

            // Assert: null = no new salt in login mode
            newSalt.Should().BeNull();
        }

        [Fact]
        public void HashPassword_EmptyPassword_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _sut.HashPassword("", null, out _));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // TOKEN SERVICE TESTS
    // Pure unit tests — no mocks or BuildMock needed (no EF Core calls)
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for TokenService.
    /// Verifies JWT structure, claims, expiry — no database involved.
    /// </summary>
    public class TokenServiceTests
    {
        private readonly TokenService _sut;
        private const string TestJwtKey = "test-super-secret-jwt-key-at-least-32-chars-long-for-hmacsha256";

        public TokenServiceTests()
        {
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configMock.Setup(c => c["Keys:Jwt"]).Returns(TestJwtKey);
            _sut = new TokenService(configMock.Object);
        }

        [Fact]
        public void CreateToken_ReturnsNonEmptyString()
        {
            var payload = new HotelBookingAppWebApi.Models.DTOs.Auth.TokenPayloadDto
            {
                UserId = Guid.NewGuid(),
                UserName = "Test User",
                Role = "Guest"
            };

            var token = _sut.CreateToken(payload);

            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CreateToken_ProducesValidJwtFormat()
        {
            var payload = new HotelBookingAppWebApi.Models.DTOs.Auth.TokenPayloadDto
            {
                UserId = Guid.NewGuid(),
                UserName = "John Doe",
                Role = "Admin",
                HotelId = Guid.NewGuid()
            };

            var token = _sut.CreateToken(payload);

            // Valid JWT = 3 dot-separated parts: header.payload.signature
            token.Split('.').Should().HaveCount(3);
        }

        [Fact]
        public void CreateToken_AdminWithHotelId_IncludesHotelIdInToken()
        {
            var hotelId = Guid.NewGuid();
            var payload = new HotelBookingAppWebApi.Models.DTOs.Auth.TokenPayloadDto
            {
                UserId = Guid.NewGuid(),
                UserName = "Hotel Owner",
                Role = "Admin",
                HotelId = hotelId
            };

            var token = _sut.CreateToken(payload);

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var hotelClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "HotelId");

            hotelClaim.Should().NotBeNull();
            hotelClaim!.Value.Should().Be(hotelId.ToString());
        }

        [Fact]
        public void CreateToken_GuestWithoutHotelId_DoesNotIncludeHotelIdClaim()
        {
            var payload = new HotelBookingAppWebApi.Models.DTOs.Auth.TokenPayloadDto
            {
                UserId = Guid.NewGuid(),
                UserName = "Guest User",
                Role = "Guest",
                HotelId = null   // Guests have no hotel
            };

            var token = _sut.CreateToken(payload);

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var hotelClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "HotelId");

            hotelClaim.Should().BeNull();
        }

        [Fact]
        public void CreateToken_TokenExpiresInOneDay()
        {
            var payload = new HotelBookingAppWebApi.Models.DTOs.Auth.TokenPayloadDto
            {
                UserId = Guid.NewGuid(),
                UserName = "Alice",
                Role = "Guest"
            };

            var token = _sut.CreateToken(payload);

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.ValidTo.Should().BeCloseTo(
                DateTime.UtcNow.AddDays(1),
                precision: TimeSpan.FromMinutes(1));
        }
    }
}