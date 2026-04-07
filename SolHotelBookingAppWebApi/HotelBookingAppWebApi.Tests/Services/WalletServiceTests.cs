using FluentAssertions;
using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Services;
using MockQueryable.Moq;
using Moq;

namespace HotelBookingAppWebApi.Tests.Services;

public class WalletServiceTests
{
    private readonly Mock<IRepository<Guid, Wallet>> _walletRepoMock = new();
    private readonly Mock<IRepository<Guid, WalletTransaction>> _walletTxRepoMock = new();
    private readonly Mock<IRepository<Guid, User>> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private WalletService CreateSut() => new(
        _walletRepoMock.Object, _walletTxRepoMock.Object,
        _userRepoMock.Object, _unitOfWorkMock.Object);

    private static Wallet MakeWallet(Guid userId, decimal balance = 500m) => new()
    {
        WalletId = Guid.NewGuid(), UserId = userId, Balance = balance, UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task EnsureWalletExistsAsync_WalletExists_DoesNotCreate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallets = new List<Wallet> { MakeWallet(userId) }.AsQueryable().BuildMock();
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(wallets);
        var sut = CreateSut();

        // Act
        await sut.EnsureWalletExistsAsync(userId);

        // Assert
        _walletRepoMock.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task EnsureWalletExistsAsync_WalletMissing_CreatesWallet()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyWallets = new List<Wallet>().AsQueryable().BuildMock();
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(emptyWallets);
        _walletRepoMock.Setup(r => r.AddAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet w) => w);
        var sut = CreateSut();

        // Act
        await sut.EnsureWalletExistsAsync(userId);

        // Assert
        _walletRepoMock.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Once);
    }

    [Fact]
    public async Task TopUpAsync_PositiveAmount_ReturnsUpdatedBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 100m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(wallets);
        _walletTxRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync((WalletTransaction wt) => wt);
        var sut = CreateSut();

        // Act
        var result = await sut.TopUpAsync(userId, 200m);

        // Assert
        result.Balance.Should().Be(300m);
    }

    [Fact]
    public async Task TopUpAsync_ZeroAmount_ThrowsValidationException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.TopUpAsync(Guid.NewGuid(), 0);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreditAsync_ValidAmount_IncrementsBalance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 100m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(wallets);
        _walletTxRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync((WalletTransaction wt) => wt);
        var sut = CreateSut();

        // Act
        await sut.CreditAsync(userId, 50m, "Reward");

        // Assert
        wallet.Balance.Should().Be(150m);
    }

    [Fact]
    public async Task DeductAsync_SufficientBalance_ReturnsTrueAndDeducts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 500m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(wallets);
        _walletTxRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync((WalletTransaction wt) => wt);
        var sut = CreateSut();

        // Act
        var result = await sut.DeductAsync(userId, 200m, "Payment");

        // Assert
        result.Should().BeTrue();
        wallet.Balance.Should().Be(300m);
    }

    [Fact]
    public async Task DeductAsync_InsufficientBalance_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 50m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(wallets);
        var sut = CreateSut();

        // Act
        var result = await sut.DeductAsync(userId, 200m, "Payment");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetGuestWalletByAdminAsync_ValidAdmin_ReturnsWallet()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var admin = new User { UserId = adminId, Name = "Admin", Email = "a@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Admin, CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var wallet = MakeWallet(guestId, 300m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(wallets);
        var sut = CreateSut();

        // Act
        var result = await sut.GetGuestWalletByAdminAsync(adminId, guestId);

        // Assert
        result.Balance.Should().Be(300m);
    }

    [Fact]
    public async Task GetGuestWalletByAdminAsync_NonAdmin_ThrowsUnAuthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var guest = new User { UserId = userId, Name = "Guest", Email = "g@b.com", Password = new byte[]{1}, PasswordSaltValue = new byte[]{2}, Role = UserRole.Guest, CreatedAt = DateTime.UtcNow };
        _userRepoMock.Setup(r => r.GetAsync(userId)).ReturnsAsync(guest);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.GetGuestWalletByAdminAsync(userId, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<UnAuthorizedException>();
    }
}
