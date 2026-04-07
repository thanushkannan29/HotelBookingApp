using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Auth;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepoMock = new();
    private readonly Mock<IRepository<Guid, UserProfileDetails>> _profileRepoMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IWalletService> _walletServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private AuthService CreateSut() => new(
        _userRepoMock.Object,
        _hotelRepoMock.Object,
        _profileRepoMock.Object,
        _passwordServiceMock.Object,
        _tokenServiceMock.Object,
        _walletServiceMock.Object,
        _unitOfWorkMock.Object);

    private void SetupEmptyUserQueryable()
    {
        var empty = new List<User>().AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(empty);
    }

    private void SetupExistingUserQueryable(User user)
    {
        var users = new List<User> { user }.AsQueryable().BuildMock();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users);
    }

    private void SetupPasswordService()
    {
        var salt = new byte[] { 1, 2, 3 };
        var hash = new byte[] { 4, 5, 6 };
        byte[]? outSalt = salt;
        _passwordServiceMock
            .Setup(p => p.HashPassword(It.IsAny<string>(), null, out outSalt))
            .Returns(hash);
        _passwordServiceMock
            .Setup(p => p.HashPassword(It.IsAny<string>(), It.IsAny<byte[]>(), out It.Ref<byte[]?>.IsAny))
            .Returns(hash);
    }

    // ── RegisterGuestAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterGuestAsync_ValidDto_ReturnsAuthResponseDto()
    {
        // Arrange
        SetupEmptyUserQueryable();
        SetupPasswordService();
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _profileRepoMock.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>())).ReturnsAsync((UserProfileDetails p) => p);
        _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("jwt-token");
        var sut = CreateSut();
        var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };

        // Act
        var result = await sut.RegisterGuestAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task RegisterGuestAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var existing = new User { UserId = Guid.NewGuid(), Email = "alice@test.com", Name = "Alice",
            Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow };
        SetupExistingUserQueryable(existing);
        var sut = CreateSut();
        var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };

        // Act
        var act = async () => await sut.RegisterGuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already registered*");
    }

    [Fact]
    public async Task RegisterGuestAsync_InnerException_CallsRollback()
    {
        // Arrange
        SetupEmptyUserQueryable();
        SetupPasswordService();
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();
        var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };

        // Act
        var act = async () => await sut.RegisterGuestAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── RegisterHotelAdminAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RegisterHotelAdminAsync_ValidDto_CreatesHotelAndAdmin()
    {
        // Arrange
        SetupEmptyUserQueryable();
        SetupPasswordService();
        _hotelRepoMock.Setup(r => r.AddAsync(It.IsAny<Hotel>())).ReturnsAsync((Hotel h) => h);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _profileRepoMock.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>())).ReturnsAsync((UserProfileDetails p) => p);
        _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("admin-jwt");
        var sut = CreateSut();
        var dto = new RegisterHotelAdminDto
        {
            Name = "Admin", Email = "admin@hotel.com", Password = "pass123",
            HotelName = "Grand Hotel", Address = "123 Main", City = "Mumbai",
            State = "MH", ContactNumber = "9999999999"
        };

        // Act
        var result = await sut.RegisterHotelAdminAsync(dto);

        // Assert
        result.Token.Should().Be("admin-jwt");
        _hotelRepoMock.Verify(r => r.AddAsync(It.IsAny<Hotel>()), Times.Once);
    }

    [Fact]
    public async Task RegisterHotelAdminAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var existing = new User { UserId = Guid.NewGuid(), Email = "admin@hotel.com", Name = "Admin",
            Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow };
        SetupExistingUserQueryable(existing);
        var sut = CreateSut();
        var dto = new RegisterHotelAdminDto
        {
            Name = "Admin", Email = "admin@hotel.com", Password = "pass123",
            HotelName = "Grand Hotel", Address = "123 Main", City = "Mumbai", ContactNumber = "9999999999"
        };

        // Act
        var act = async () => await sut.RegisterHotelAdminAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RegisterHotelAdminAsync_InnerException_CallsRollback()
    {
        // Arrange
        SetupEmptyUserQueryable();
        SetupPasswordService();
        _hotelRepoMock.Setup(r => r.AddAsync(It.IsAny<Hotel>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();
        var dto = new RegisterHotelAdminDto
        {
            Name = "Admin", Email = "admin@hotel.com", Password = "pass123",
            HotelName = "Grand Hotel", Address = "123 Main", City = "Mumbai", ContactNumber = "9999999999"
        };

        // Act
        var act = async () => await sut.RegisterHotelAdminAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponseDto()
    {
        // Arrange
        var salt = new byte[] { 1, 2, 3 };
        var hash = new byte[] { 4, 5, 6 };
        var user = new User
        {
            UserId = Guid.NewGuid(), Email = "alice@test.com", Name = "Alice",
            Password = hash, PasswordSaltValue = salt, IsActive = true,
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow
        };
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        byte[]? outSalt = null;
        _passwordServiceMock.Setup(p => p.HashPassword("pass123", salt, out outSalt)).Returns(hash);
        _tokenServiceMock.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("jwt");
        var sut = CreateSut();

        // Act
        var result = await sut.LoginAsync(new LoginDto { Email = "alice@test.com", Password = "pass123" });

        // Assert
        result.Token.Should().Be("jwt");
    }

    [Fact]
    public async Task LoginAsync_EmailNotFound_ThrowsUnAuthorizedException()
    {
        // Arrange
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.LoginAsync(new LoginDto { Email = "nobody@test.com", Password = "pass" });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_AccountDeactivated_ThrowsUnAuthorizedException()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(), Email = "alice@test.com", Name = "Alice",
            Password = new byte[]{1}, PasswordSaltValue = new byte[]{2},
            IsActive = false, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow
        };
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.LoginAsync(new LoginDto { Email = "alice@test.com", Password = "pass" });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>().WithMessage("*deactivated*");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnAuthorizedException()
    {
        // Arrange
        var salt = new byte[] { 1, 2, 3 };
        var correctHash = new byte[] { 4, 5, 6 };
        var wrongHash = new byte[] { 7, 8, 9 };
        var user = new User
        {
            UserId = Guid.NewGuid(), Email = "alice@test.com", Name = "Alice",
            Password = correctHash, PasswordSaltValue = salt, IsActive = true,
            Role = UserRole.Guest, CreatedAt = DateTime.UtcNow
        };
        _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);
        byte[]? outSalt = null;
        _passwordServiceMock.Setup(p => p.HashPassword("wrongpass", salt, out outSalt)).Returns(wrongHash);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.LoginAsync(new LoginDto { Email = "alice@test.com", Password = "wrongpass" });

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }
}
