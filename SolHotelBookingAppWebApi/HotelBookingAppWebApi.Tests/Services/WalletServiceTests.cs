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
    private readonly Mock<IRepository<Guid, Wallet>> _walletRepo = new();
    private readonly Mock<IRepository<Guid, WalletTransaction>> _txRepo = new();
    private readonly Mock<IRepository<Guid, User>> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly WalletService _sut;

    public WalletServiceTests()
    {
        _sut = new WalletService(_walletRepo.Object, _txRepo.Object, _userRepo.Object, _unitOfWork.Object);
    }

    private Wallet MakeWallet(Guid userId, decimal balance = 500m)
        => new() { WalletId = Guid.NewGuid(), UserId = userId, Balance = balance, UpdatedAt = DateTime.UtcNow };

    // ── EnsureWalletExistsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EnsureWalletExistsAsync_WalletExists_DoesNotCreate()
    {
        var userId = Guid.NewGuid();
        var wallets = new List<Wallet> { MakeWallet(userId) }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        await _sut.EnsureWalletExistsAsync(userId);

        _walletRepo.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task EnsureWalletExistsAsync_NoWallet_CreatesOne()
    {
        var userId = Guid.NewGuid();
        var wallets = new List<Wallet>().AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        _walletRepo.Setup(r => r.AddAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet w) => w);

        await _sut.EnsureWalletExistsAsync(userId);

        _walletRepo.Verify(r => r.AddAsync(It.Is<Wallet>(w => w.UserId == userId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── TopUpAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TopUpAsync_ValidAmount_IncreasesBalance()
    {
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 100m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        _txRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

        var result = await _sut.TopUpAsync(userId, 200m);

        result.Balance.Should().Be(300m);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task TopUpAsync_ZeroAmount_ThrowsValidation()
    {
        await _sut.Invoking(s => s.TopUpAsync(Guid.NewGuid(), 0))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task TopUpAsync_NegativeAmount_ThrowsValidation()
    {
        await _sut.Invoking(s => s.TopUpAsync(Guid.NewGuid(), -50))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task TopUpAsync_OnException_Rollback()
    {
        var userId = Guid.NewGuid();
        var wallets = new List<Wallet>().AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        _walletRepo.Setup(r => r.AddAsync(It.IsAny<Wallet>())).ThrowsAsync(new Exception("db"));

        await _sut.Invoking(s => s.TopUpAsync(userId, 100)).Should().ThrowAsync<Exception>();
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── CreditAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_AddsToBalance()
    {
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 50m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        _txRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

        await _sut.CreditAsync(userId, 150m, "reward");

        wallet.Balance.Should().Be(200m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreditAsync_NoWallet_CreatesWalletAndCredits()
    {
        var userId = Guid.NewGuid();
        var wallets = new List<Wallet>().AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        var created = new Wallet { WalletId = Guid.NewGuid(), UserId = userId, Balance = 0 };
        _walletRepo.Setup(r => r.AddAsync(It.IsAny<Wallet>())).ReturnsAsync((Wallet w) => { created = w; return w; });
        _txRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

        await _sut.CreditAsync(userId, 100m, "test");

        created.Balance.Should().Be(100m);
    }

    // ── DeductAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeductAsync_SufficientBalance_ReturnsTrueAndDeducts()
    {
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 300m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        _txRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

        var result = await _sut.DeductAsync(userId, 100m, "deduct");

        result.Should().BeTrue();
        wallet.Balance.Should().Be(200m);
    }

    [Fact]
    public async Task DeductAsync_InsufficientBalance_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 50m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        var result = await _sut.DeductAsync(userId, 100m, "deduct");

        result.Should().BeFalse();
        wallet.Balance.Should().Be(50m);
    }

    // ── DebitAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DebitAsync_DebitsUpToBalance()
    {
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 80m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        _txRepo.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

        var result = await _sut.DebitAsync(userId, 200m, "debit");

        result.Should().BeTrue();
        wallet.Balance.Should().Be(0m);
    }

    [Fact]
    public async Task DebitAsync_ZeroBalance_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 0m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        var result = await _sut.DebitAsync(userId, 50m, "debit");

        result.Should().BeFalse();
    }

    // ── GetWalletAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWalletAsync_ReturnsPagedTransactions()
    {
        var userId = Guid.NewGuid();
        var wallet = MakeWallet(userId, 200m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);
        var txs = new List<WalletTransaction>
        {
            new() { WalletTransactionId = Guid.NewGuid(), WalletId = wallet.WalletId, Amount = 100, Type = "Credit", Description = "top-up", CreatedAt = DateTime.UtcNow }
        }.AsQueryable().BuildMock();
        _txRepo.Setup(r => r.GetQueryable()).Returns(txs);

        var result = await _sut.GetWalletAsync(userId, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Wallet.Balance.Should().Be(200m);
    }

    // ── GetGuestWalletByAdminAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetGuestWalletByAdminAsync_AdminRole_ReturnsWallet()
    {
        var adminId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var admin = new User { UserId = adminId, Role = UserRole.Admin, Name = "Admin", Email = "a@a.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(adminId)).ReturnsAsync(admin);
        var wallet = MakeWallet(guestId, 500m);
        var wallets = new List<Wallet> { wallet }.AsQueryable().BuildMock();
        _walletRepo.Setup(r => r.GetQueryable()).Returns(wallets);

        var result = await _sut.GetGuestWalletByAdminAsync(adminId, guestId);

        result.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task GetGuestWalletByAdminAsync_NonAdmin_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var guest = new User { UserId = userId, Role = UserRole.Guest, Name = "G", Email = "g@g.com", CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.GetAsync(userId)).ReturnsAsync(guest);

        await _sut.Invoking(s => s.GetGuestWalletByAdminAsync(userId, Guid.NewGuid()))
            .Should().ThrowAsync<UnAuthorizedException>();
    }

    [Fact]
    public async Task GetGuestWalletByAdminAsync_AdminNotFound_ThrowsUnauthorized()
    {
        _userRepo.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetGuestWalletByAdminAsync(Guid.NewGuid(), Guid.NewGuid()))
            .Should().ThrowAsync<UnAuthorizedException>();
    }
}
