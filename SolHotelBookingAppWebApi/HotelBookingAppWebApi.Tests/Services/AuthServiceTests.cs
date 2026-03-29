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
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IRepository<Guid, Hotel>> _hotelRepo = new();
    private readonly Mock<IRepository<Guid, UserProfileDetails>> _profileRepo = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IWalletService> _walletService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepo.Object, _hotelRepo.Object, _profileRepo.Object,
            _passwordService.Object, _tokenService.Object,
            _walletService.Object, _unitOfWork.Object);
    }

    // ── RegisterGuestAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterGuestAsync_NewEmail_ReturnsToken()
    {
        var dto = new RegisterUserDto { Name = "Alice", Email = "alice@test.com", Password = "pass123" };
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);
        _passwordService.Setup(p => p.HashPassword(dto.Password, null, out It.Ref<byte[]?>.IsAny))
            .Returns((string _, byte[]? _, out byte[]? salt) => { salt = new byte[] { 1 }; return new byte[] { 2 }; });
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _profileRepo.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>())).ReturnsAsync(new UserProfileDetails());
        _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("jwt-token");

        var result = await _sut.RegisterGuestAsync(dto);

        result.Token.Should().Be("jwt-token");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _walletService.Verify(w => w.EnsureWalletExistsAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task RegisterGuestAsync_DuplicateEmail_ThrowsConflict()
    {
        var dto = new RegisterUserDto { Name = "Bob", Email = "bob@test.com", Password = "pass" };
        var existing = new List<User> { new() { Email = "bob@test.com" } }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(existing);

        await _sut.Invoking(s => s.RegisterGuestAsync(dto))
            .Should().ThrowAsync<ConflictException>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterGuestAsync_OnException_Rollback()
    {
        var dto = new RegisterUserDto { Name = "X", Email = "x@test.com", Password = "p" };
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);
        _passwordService.Setup(p => p.HashPassword(It.IsAny<string>(), null, out It.Ref<byte[]?>.IsAny))
            .Returns((string _, byte[]? _, out byte[]? salt) => { salt = new byte[] { 1 }; return new byte[] { 2 }; });
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ThrowsAsync(new Exception("db error"));

        await _sut.Invoking(s => s.RegisterGuestAsync(dto)).Should().ThrowAsync<Exception>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── RegisterHotelAdminAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RegisterHotelAdminAsync_NewEmail_ReturnsToken()
    {
        var dto = new RegisterHotelAdminDto
        {
            Name = "Admin", Email = "admin@hotel.com", Password = "pass",
            HotelName = "Grand", Address = "123 St", City = "NYC",
            State = "NY", Description = "Luxury", ContactNumber = "1234567890"
        };
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);
        _hotelRepo.Setup(r => r.AddAsync(It.IsAny<Hotel>())).ReturnsAsync((Hotel h) => h);
        _passwordService.Setup(p => p.HashPassword(dto.Password, null, out It.Ref<byte[]?>.IsAny))
            .Returns((string _, byte[]? _, out byte[]? salt) => { salt = new byte[] { 1 }; return new byte[] { 2 }; });
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _profileRepo.Setup(r => r.AddAsync(It.IsAny<UserProfileDetails>())).ReturnsAsync(new UserProfileDetails());
        _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("admin-token");

        var result = await _sut.RegisterHotelAdminAsync(dto);

        result.Token.Should().Be("admin-token");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterHotelAdminAsync_DuplicateEmail_ThrowsConflict()
    {
        var dto = new RegisterHotelAdminDto { Email = "dup@hotel.com", Name = "X", Password = "p", HotelName = "H", Address = "A", City = "C", ContactNumber = "123" };
        var existing = new List<User> { new() { Email = "dup@hotel.com" } }.AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(existing);

        await _sut.Invoking(s => s.RegisterHotelAdminAsync(dto)).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RegisterHotelAdminAsync_OnException_Rollback()
    {
        var dto = new RegisterHotelAdminDto { Email = "err@hotel.com", Name = "X", Password = "p", HotelName = "H", Address = "A", City = "C", ContactNumber = "123" };
        var users = new List<User>().AsQueryable().BuildMock();
        _userRepo.Setup(r => r.GetQueryable()).Returns(users);
        _hotelRepo.Setup(r => r.AddAsync(It.IsAny<Hotel>())).ThrowsAsync(new Exception("fail"));

        await _sut.Invoking(s => s.RegisterHotelAdminAsync(dto)).Should().ThrowAsync<Exception>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var hash = new byte[] { 99 };
        var salt = new byte[] { 1 };
        var user = new User { UserId = Guid.NewGuid(), Email = "u@test.com", Password = hash, PasswordSaltValue = salt, IsActive = true, Role = UserRole.Guest, Name = "U", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(user);
        _passwordService.Setup(p => p.HashPassword("pass", salt, out It.Ref<byte[]?>.IsAny)).Returns((string _, byte[]? _, out byte[]? s) => { s = null; return hash; });
        _tokenService.Setup(t => t.CreateToken(It.IsAny<TokenPayloadDto>())).Returns("login-token");

        var result = await _sut.LoginAsync(new LoginDto { Email = "u@test.com", Password = "pass" });

        result.Token.Should().Be("login-token");
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.LoginAsync(new LoginDto { Email = "no@test.com", Password = "p" }))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorized()
    {
        var user = new User { IsActive = false, Password = new byte[] { 1 }, PasswordSaltValue = new byte[] { 1 }, Name = "X", Email = "x@x.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(user);

        await _sut.Invoking(s => s.LoginAsync(new LoginDto { Email = "x@x.com", Password = "p" }))
            .Should().ThrowAsync<UnAuthorizedException>().WithMessage("*deactivated*");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var hash = new byte[] { 99 };
        var salt = new byte[] { 1 };
        var user = new User { IsActive = true, Password = hash, PasswordSaltValue = salt, Name = "X", Email = "x@x.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(user);
        _passwordService.Setup(p => p.HashPassword("wrong", salt, out It.Ref<byte[]?>.IsAny)).Returns((string _, byte[]? _, out byte[]? s) => { s = null; return new byte[] { 0 }; });

        await _sut.Invoking(s => s.LoginAsync(new LoginDto { Email = "x@x.com", Password = "wrong" }))
            .Should().ThrowAsync<UnAuthorizedException>().WithMessage("*credentials*");
    }
}
