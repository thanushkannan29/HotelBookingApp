using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Manages guest wallet balance and transaction history.
    /// All balance mutations go through private helpers to enforce consistency.
    /// </summary>
    public class WalletService : IWalletService
    {
        private readonly IRepository<Guid, Wallet> _walletRepo;
        private readonly IRepository<Guid, WalletTransaction> _walletTransactionRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;

        public WalletService(
            IRepository<Guid, Wallet> walletRepo,
            IRepository<Guid, WalletTransaction> walletTransactionRepo,
            IRepository<Guid, User> userRepo,
            IUnitOfWork unitOfWork)
        {
            _walletRepo = walletRepo;
            _walletTransactionRepo = walletTransactionRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        // ── PUBLIC API ────────────────────────────────────────────────────────

        public async Task EnsureWalletExistsAsync(Guid userId)
        {
            var exists = await _walletRepo.GetQueryable().AnyAsync(w => w.UserId == userId);
            if (!exists) await CreateWalletAsync(userId);
        }

        public async Task<PagedWalletTransactionDto> GetWalletAsync(Guid userId, int page, int pageSize)
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            var query = _walletTransactionRepo.GetQueryable()
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedWalletTransactionDto
            {
                TotalCount = total,
                Wallet = MapWalletToDto(wallet),
                Transactions = items.Select(MapTransactionToDto)
            };
        }

        public async Task<WalletResponseDto> TopUpAsync(Guid userId, decimal amount)
        {
            if (amount <= 0) throw new ValidationException("Top-up amount must be positive.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await GetOrCreateWalletAsync(userId);
                await ApplyCreditAsync(wallet, amount, $"Wallet top-up of ₹{amount}");
                await _unitOfWork.CommitAsync();
                return MapWalletToDto(wallet);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task CreditAsync(Guid userId, decimal amount, string description)
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            await ApplyCreditAsync(wallet, amount, description);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<bool> DeductAsync(Guid userId, decimal amount, string description)
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            if (wallet.Balance < amount) return false;
            await ApplyDebitAsync(wallet, amount, description);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>Debits up to the available balance — never throws on insufficient funds.</summary>
        public async Task<bool> DebitAsync(Guid userId, decimal amount, string description)
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            var actualDebit = Math.Min(amount, wallet.Balance);
            if (actualDebit <= 0) return false;
            await ApplyDebitAsync(wallet, actualDebit, description);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<WalletResponseDto> GetGuestWalletByAdminAsync(Guid adminUserId, Guid guestUserId)
        {
            await EnsureAdminRoleAsync(adminUserId);
            var wallet = await GetOrCreateWalletAsync(guestUserId);
            return MapWalletToDto(wallet);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<Wallet> GetOrCreateWalletAsync(Guid userId)
        {
            var wallet = await _walletRepo.GetQueryable().FirstOrDefaultAsync(w => w.UserId == userId);
            return wallet ?? await CreateWalletAsync(userId);
        }

        private async Task<Wallet> CreateWalletAsync(Guid userId)
        {
            var wallet = new Wallet
            {
                WalletId = Guid.NewGuid(),
                UserId = userId,
                Balance = 0,
                UpdatedAt = DateTime.UtcNow
            };
            await _walletRepo.AddAsync(wallet);
            await _unitOfWork.SaveChangesAsync();
            return wallet;
        }

        private async Task ApplyCreditAsync(Wallet wallet, decimal amount, string description)
        {
            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            await RecordWalletTransactionAsync(wallet.WalletId, amount, "Credit", description);
        }

        private async Task ApplyDebitAsync(Wallet wallet, decimal amount, string description)
        {
            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            await RecordWalletTransactionAsync(wallet.WalletId, amount, "Debit", description);
        }

        private async Task RecordWalletTransactionAsync(
            Guid walletId, decimal amount, string type, string description)
        {
            await _walletTransactionRepo.AddAsync(new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = walletId,
                Amount = amount,
                Type = type,
                Description = description,
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task EnsureAdminRoleAsync(Guid adminUserId)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.Role != UserRole.Admin)
                throw new UnAuthorizedException("Unauthorized.");
        }

        private static WalletResponseDto MapWalletToDto(Wallet wallet) => new()
        {
            WalletId = wallet.WalletId,
            Balance = wallet.Balance,
            UpdatedAt = wallet.UpdatedAt
        };

        private static WalletTransactionDto MapTransactionToDto(WalletTransaction transaction) => new()
        {
            WalletTransactionId = transaction.WalletTransactionId,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        };
    }
}
