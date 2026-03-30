using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Repository
{
    /// <summary>
    /// Generic EF Core repository implementation.
    /// Provides standard CRUD and queryable access without saving changes —
    /// callers are responsible for committing via IUnitOfWork.
    /// </summary>
    public class Repository<TKey, TEntity> : IRepository<TKey, TEntity> where TEntity : class
    {
        protected readonly HotelBookingContext _context;

        public Repository(HotelBookingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<TEntity?> AddAsync(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _context.Set<TEntity>().AddAsync(entity);
            return entity;
        }

        /// <inheritdoc/>
        public async Task<TEntity?> DeleteAsync(TKey key)
        {
            var entity = await GetAsync(key);
            if (entity is null) return null;
            _context.Set<TEntity>().Remove(entity);
            return entity;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TEntity>> GetAllAsync()
            => await _context.Set<TEntity>().ToListAsync();

        /// <inheritdoc/>
        public async Task<TEntity?> GetAsync(TKey key)
            => await _context.FindAsync<TEntity>(key);

        /// <inheritdoc/>
        public async Task<TEntity?> UpdateAsync(TKey key, TEntity entity)
        {
            if (entity is null) return null;
            var existing = await GetAsync(key);
            if (existing is null) return null;
            _context.Entry(existing).CurrentValues.SetValues(entity);
            return existing;
        }

        /// <inheritdoc/>
        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
            => await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);

        /// <inheritdoc/>
        public IQueryable<TEntity> GetQueryable()
            => _context.Set<TEntity>();

        /// <inheritdoc/>
        public async Task<IEnumerable<TEntity>> GetAllByForeignKeyAsync(
            Expression<Func<TEntity, bool>> predicate, int limit, int pageNumber)
            => await _context.Set<TEntity>()
                .Where(predicate)
                .Skip((pageNumber - 1) * limit)
                .Take(limit)
                .ToListAsync();
    }
}
