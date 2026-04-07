using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Wraps EF Core's DbContext transaction lifecycle.
    /// Prevents nested transactions and provides a safe commit/rollback pattern.
    /// </summary>
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly HotelBookingContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(HotelBookingContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task BeginTransactionAsync()
        {
            if (_transaction is not null) return; // guard against nested transactions
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        /// <inheritdoc/>
        public async Task CommitAsync()
        {
            if (_transaction is null)
            {
                await _context.SaveChangesAsync(); // safe fallback — no explicit transaction
                return;
            }

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        /// <inheritdoc/>
        public async Task RollbackAsync()
        {
            if (_transaction is null) return;

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();

        public void Dispose()
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task DisposeTransactionAsync()
        {
            if (_transaction is null) return;
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
