using HotelBookingAppWebApi.Exceptions;
using HotelBookingAppWebApi.Interfaces;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using HotelBookingAppWebApi.Models;
using HotelBookingAppWebApi.Models.DTOs.Wallet;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Services
{
    public class WalletService : IWalletService
    {
        private readonly IRepository<Guid, Wallet> _walletRepo;
        private readonly IRepository<Guid, WalletTransaction> _txRepo;
        private readonly IRepository<Guid, User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;

        public WalletService(
            IRepository<Guid, Wallet> walletRepo,
            IRepository<Guid, WalletTransaction> txRepo,
            IRepository<Guid, User> userRepo,
            IUnitOfWork unitOfWork)
        {
            _walletRepo = walletRepo;
            _txRepo = txRepo;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task EnsureWalletExistsAsync(Guid userId)
        {
            var exists = await _walletRepo.GetQueryable().AnyAsync(w => w.UserId == userId);
            if (!exists)
            {
                await _walletRepo.AddAsync(new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0,
                    UpdatedAt = DateTime.UtcNow
                });
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<PagedWalletTransactionDto> GetWalletAsync(Guid userId, int page, int pageSize)
        {
            var wallet = await GetOrCreateWalletAsync(userId);

            var query = _txRepo.GetQueryable()
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedWalletTransactionDto
            {
                TotalCount = total,
                Wallet = MapWallet(wallet),
                Transactions = items.Select(MapTransaction)
            };
        }

        public async Task<WalletResponseDto> TopUpAsync(Guid userId, decimal amount)
        {
            if (amount <= 0) throw new ValidationException("Top-up amount must be positive.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await GetOrCreateWalletAsync(userId);
                wallet.Balance += amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                await _txRepo.AddAsync(new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    Amount = amount,
                    Type = "Credit",
                    Description = $"Wallet top-up of ₹{amount}",
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.CommitAsync();
                return MapWallet(wallet);
            }
            catch { await _unitOfWork.RollbackAsync(); throw; }
        }

        public async Task CreditAsync(Guid userId, decimal amount, string description)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await GetOrCreateWalletAsync(userId);
                wallet.Balance += amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                await _txRepo.AddAsync(new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    Amount = amount,
                    Type = "Credit",
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.CommitAsync();
            }
            catch { await _unitOfWork.RollbackAsync(); throw; }
        }

        public async Task<bool> DeductAsync(Guid userId, decimal amount, string description)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await GetOrCreateWalletAsync(userId);
                if (wallet.Balance < amount) return false;

                wallet.Balance -= amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                await _txRepo.AddAsync(new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    Amount = amount,
                    Type = "Debit",
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch { await _unitOfWork.RollbackAsync(); throw; }
        }

        public async Task<WalletResponseDto> GetGuestWalletByAdminAsync(Guid adminUserId, Guid guestUserId)
        {
            var admin = await _userRepo.GetAsync(adminUserId)
                ?? throw new UnAuthorizedException("Unauthorized.");
            if (admin.Role != UserRole.Admin)
                throw new UnAuthorizedException("Unauthorized.");

            var wallet = await GetOrCreateWalletAsync(guestUserId);
            return MapWallet(wallet);
        }

        // ── HELPERS ───────────────────────────────────────────────────────────
        private async Task<Wallet> GetOrCreateWalletAsync(Guid userId)
        {
            var wallet = await _walletRepo.GetQueryable()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                await _walletRepo.AddAsync(wallet);
                await _unitOfWork.SaveChangesAsync();
            }

            return wallet;
        }

        private static WalletResponseDto MapWallet(Wallet w) => new()
        {
            WalletId = w.WalletId,
            Balance = w.Balance,
            UpdatedAt = w.UpdatedAt
        };

        private static WalletTransactionDto MapTransaction(WalletTransaction t) => new()
        {
            WalletTransactionId = t.WalletTransactionId,
            Amount = t.Amount,
            Type = t.Type,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        };
    }
}
