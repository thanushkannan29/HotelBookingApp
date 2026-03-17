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
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("Transaction not started");

            await _transaction.RollbackAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}
