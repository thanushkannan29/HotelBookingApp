using Xunit;
using Moq;
using MockQueryable.Moq;          // ← ADDED: gives .BuildMock() on any List<T>
using FluentAssertions;
using HotelBookingAppWebApi.Services;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using HotelBookingAppWebApi.Exceptions;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Tests.Services
{
    /// <summary>
    /// Tests for AuthService: RegisterGuest, RegisterHotelAdmin, Login
    ///
    /// ROOT CAUSE OF PREVIOUS ERRORS:
    ///   list.AsQueryable() creates a plain in-memory queryable that does NOT support
    ///   EF Core async methods like AnyAsync(), FirstOrDefaultAsync(), ToListAsync().
    ///
    /// FIX:
    ///   Install NuGet package "MockQueryable.Moq" version 7.0.0 and call
    ///   .BuildMock() instead of .AsQueryable(). BuildMock() wraps the list in a
    ///   full async-capable queryable that satisfies EF Core's IAsyncQueryProvider.
    ///
    /// PREREQUISITE — add to HotelBookingAppWebApi.Tests.csproj:
    ///   <PackageReference Include="MockQueryable.Moq" Version="7.0.0" />
    /// </summary>
    public class AuthServiceTests
    {
        // ── MOCKS ─────────────────────────────────────────────────────────────
        private readonly Mock<IRepository<Guid, User>> _userRepoMock;
        private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock;
        private readonly Mock<IRepository<Guid, UserProfileDetails>> _profileRepoMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly AuthService _sut; // System Under Test

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IRepository<Guid, User>>();
            _hotelRepoMock = new Mock<IRepository<Guid, Hotel>>();
            _profileRepoMock = new Mock<IRepository<Guid, UserProfileDetails>>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _sut = new AuthService(
                _userRepoMock.Object,
                _hotelRepoMock.Object,
                _profileRepoMock.Object,
                _passwordServiceMock.Object,
                _tokenServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        /// <summary>
        /// FIX: Use .BuildMock() instead of .AsQueryable()
        ///
        /// WHY: AuthService calls .AnyAsync() on GetQueryable() to check email uniqueness.
        /// AnyAsync() is an EF Core extension that requires IAsyncQueryProvider.
        /// Plain List.AsQueryable() does NOT have IAsyncQueryProvider, throws InvalidOperationException.
        /// BuildMock() from MockQueryable.Moq wraps the list so it implements IAsyncQueryProvider.
        /// </summary>
        private void SetupUserQueryable(IEnumerable<User> users)
        {
            _userRepoMock.Setup(r => r.GetQueryable())
                .Returns(users.AsQueryable().BuildMock()); // ← KEY FIX: .BuildMock()
        }

        /// <summary>
        /// Sets up IPasswordService.HashPassword which has an "out" parameter.
        /// When existingSalt is null  → registration mode → outputs new salt
        /// When existingSalt provided → login verify mode → outputs null for newSalt
        /// </summary>
        private void SetupPasswordHash(byte[] hashResult, byte[] salt)
        {
            _passwordServiceMock.Setup(p => p.HashPassword(
                    It.IsAny<string>(),
                    It.IsAny<byte[]?>(),
                    out It.Ref<byte[]?>.IsAny))
                .Returns((string pwd, byte[]? existingSalt, out byte[]? newSalt) =>
                {
                    newSalt = existingSalt == null ? salt : null;
                    return hashResult;
                });
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RegisterGuestAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task RegisterGuestAsync_WithNewEmail_ReturnsToken()
        {
            // Arrange
            var dto = new RegisterUserDto
            {
                Name = "Alice",
                Email = "alice@test.com",
                Password = "pass123"
            };

            SetupUserQueryable(new List<User>());   // Empty list → email is unique
            SetupPasswordHash(new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });

            _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                             .Returns("jwt-token");
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);
            _profileRepoMock.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>()))
                            .ReturnsAsync((UserProfileDetails p) => p);

            // Act
            var result = await _sut.RegisterGuestAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("jwt-token");

            _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Email == dto.Email &&
                u.Name == dto.Name &&
                u.Role == UserRole.Guest)), Times.Once);

            _profileRepoMock.Verify(r => r.AddAsync(It.IsAny<UserProfileDetails>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterGuestAsync_WithDuplicateEmail_ThrowsConflictException()
        {
            // Arrange
            var dto = new RegisterUserDto { Name = "Bob", Email = "existing@test.com", Password = "pass123" };
            var existingUser = new User { Email = "existing@test.com" };

            SetupUserQueryable(new List<User> { existingUser }); // Email already in DB

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterGuestAsync(dto));

            // Transaction should never even start when email already exists
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task RegisterGuestAsync_WhenCommitFails_CallsRollback()
        {
            // Arrange
            var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };

            SetupUserQueryable(new List<User>());
            SetupPasswordHash(new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });

            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);
            _profileRepoMock.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>()))
                            .ReturnsAsync((UserProfileDetails p) => p);
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _sut.RegisterGuestAsync(dto));

            // Rollback MUST be called when commit throws
            _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterGuestAsync_CreatesProfileWithPlaceholderValues()
        {
            // Arrange
            var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };

            SetupUserQueryable(new List<User>());
            SetupPasswordHash(new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });

            _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                             .Returns("jwt-token");
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);
            _profileRepoMock.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>()))
                            .ReturnsAsync((UserProfileDetails p) => p);

            // Act
            await _sut.RegisterGuestAsync(dto);

            // Assert: Profile must be created with "Not Updated" placeholder values
            // because the guest only provides Name/Email/Password at registration
            _profileRepoMock.Verify(r => r.AddAsync(It.Is<UserProfileDetails>(p =>
                p.PhoneNumber == "Not Updated" &&
                p.Address == "Not Updated" &&
                p.Email == dto.Email &&
                p.Name == dto.Name)), Times.Once);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // RegisterHotelAdminAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task RegisterHotelAdminAsync_WithValidData_CreatesHotelAndAdminAtomically()
        {
            // Arrange
            var dto = new RegisterHotelAdminDto
            {
                Name = "Owner",
                Email = "owner@hotel.com",
                Password = "secure123",
                HotelName = "Grand Hotel",
                Address = "123 Main St",
                City = "Mumbai",
                ContactNumber = "9876543210"
            };

            SetupUserQueryable(new List<User>());
            SetupPasswordHash(new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });

            _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                             .Returns("admin-jwt");
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
            _hotelRepoMock.Setup(r => r.AddAsync(It.IsAny<Hotel>()))
                          .ReturnsAsync((Hotel h) => h);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                         .ReturnsAsync((User u) => u);
            _profileRepoMock.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>()))
                            .ReturnsAsync((UserProfileDetails p) => p);

            // Act
            var result = await _sut.RegisterHotelAdminAsync(dto);

            // Assert
            result.Token.Should().Be("admin-jwt");

            // Hotel must be created with the correct name and city
            _hotelRepoMock.Verify(r => r.AddAsync(It.Is<Hotel>(h =>
                h.Name == dto.HotelName &&
                h.City == dto.City)), Times.Once);

            // User must be Admin role with HotelId linked
            _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.Role == UserRole.Admin &&
                u.HotelId != null)), Times.Once);
        }

        [Fact]
        public async Task RegisterHotelAdminAsync_WithDuplicateEmail_ThrowsConflictException()
        {
            // Arrange
            var dto = new RegisterHotelAdminDto { Email = "existing@hotel.com" };

            SetupUserQueryable(new List<User> { new User { Email = "existing@hotel.com" } });

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterHotelAdminAsync(dto));

            // Hotel must NOT be created if the email already exists
            _hotelRepoMock.Verify(r => r.AddAsync(It.IsAny<Hotel>()), Times.Never);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // LoginAsync Tests
        // ═══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task LoginAsync_WithCorrectCredentials_ReturnsToken()
        {
            // Arrange
            var password = "correct-password";
            var salt = new byte[] { 10, 20, 30 };
            var hash = new byte[] { 1, 2, 3 };

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "user@test.com",
                Password = hash,
                PasswordSaltValue = salt,
                IsActive = true,
                Role = UserRole.Guest
            };
            var dto = new LoginDto { Email = "user@test.com", Password = password };

            // NOTE: Login uses FirstOrDefaultAsync (not GetQueryable)
            // so standard Moq .ReturnsAsync() works here — no BuildMock needed
            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user);

            _passwordServiceMock.Setup(p => p.HashPassword(password, salt, out It.Ref<byte[]?>.IsAny))
                .Returns((string pwd, byte[]? s, out byte[]? ns) => { ns = null; return hash; });

            _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                             .Returns("login-token");

            // Act
            var result = await _sut.LoginAsync(dto);

            // Assert
            result.Token.Should().Be("login-token");
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ThrowsUnAuthorizedException()
        {
            // Arrange
            var salt = new byte[] { 10, 20, 30 };
            var storedHash = new byte[] { 1, 2, 3 };
            var wrongHash = new byte[] { 9, 9, 9 }; // Different from storedHash → mismatch

            var user = new User
            {
                Email = "user@test.com",
                Password = storedHash,
                PasswordSaltValue = salt,
                IsActive = true
            };
            var dto = new LoginDto { Email = "user@test.com", Password = "wrong-password" };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user);

            _passwordServiceMock.Setup(p => p.HashPassword("wrong-password", salt, out It.Ref<byte[]?>.IsAny))
                .Returns((string pwd, byte[]? s, out byte[]? ns) => { ns = null; return wrongHash; });

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(() => _sut.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentEmail_ThrowsUnAuthorizedException()
        {
            // Arrange: FirstOrDefaultAsync returns null → user not found in DB
            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync((User?)null);

            var dto = new LoginDto { Email = "nobody@test.com", Password = "anypassword" };

            // Act & Assert
            await Assert.ThrowsAsync<UnAuthorizedException>(() => _sut.LoginAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_WithDeactivatedAccount_ThrowsUnAuthorizedException()
        {
            // Arrange
            var user = new User
            {
                Email = "user@test.com",
                Password = new byte[] { 1 },
                PasswordSaltValue = new byte[] { 2 },
                IsActive = false // Account has been deactivated
            };
            var dto = new LoginDto { Email = "user@test.com", Password = "password" };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(user);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UnAuthorizedException>(() => _sut.LoginAsync(dto));
            ex.Message.Should().Contain("deactivated");
        }

        [Fact]
        public async Task LoginAsync_GeneratesTokenWithCorrectPayload()
        {
            // Arrange: Admin user — token must include HotelId claim
            var userId = Guid.NewGuid();
            var hotelId = Guid.NewGuid();
            var salt = new byte[] { 1 };
            var hash = new byte[] { 2 };

            var admin = new User
            {
                UserId = userId,
                Name = "Hotel Owner",
                Email = "admin@hotel.com",
                Password = hash,
                PasswordSaltValue = salt,
                IsActive = true,
                Role = UserRole.Admin,
                HotelId = hotelId
            };
            var dto = new LoginDto { Email = "admin@hotel.com", Password = "password" };

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                         .ReturnsAsync(admin);

            _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>(), salt, out It.Ref<byte[]?>.IsAny))
                .Returns((string pwd, byte[]? s, out byte[]? ns) => { ns = null; return hash; });

            _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>()))
                             .Returns("token");

            // Act
            await _sut.LoginAsync(dto);

            // Assert: Token payload must contain UserId, Role=Admin, and HotelId
            _tokenServiceMock.Verify(t => t.CreateToken(It.Is<TokenPayloadDto>(p =>
                p.UserId == userId &&
                p.Role == "Admin" &&
                p.HotelId == hotelId)), Times.Once);
        }
    }
}