using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces.UnitOfWorkInterface;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelBookingAppWebApi.Services
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly HotelBookingContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(HotelBookingContext context)
        {
            _context = context;
        }

        public async Task BeginTransactionAsync()
        {
            // Guard: don't start a nested transaction
            if (_transaction != null) return;
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            if (_transaction == null)
            {
                // No explicit transaction — just save changes (safe fallback)
                await _context.SaveChangesAsync();
                return;
            }

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction == null) return;

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();

        public void Dispose()
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }
}
