using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces.RepositoryInterface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Repository
{
    public class Repository<K, C> : IRepository<K, C> where C : class
    {
        protected readonly HotelBookingContext _context;

        public Repository(HotelBookingContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        //  ADD (NO SaveChanges)
        public async Task<C?> AddAsync(C entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _context.Set<C>().AddAsync(entity);

            return entity;
        }

        //  DELETE (NO SaveChanges)
        public async Task<C?> DeleteAsync(K key)
        {
            var item = await GetAsync(key);

            if (item == null)
                return null;

            _context.Set<C>().Remove(item);

            return item;
        }

        //  GET ALL
        public async Task<IEnumerable<C>> GetAllAsync()
        {
            return await _context.Set<C>().ToListAsync();
        }

        //  GET BY ID
        public async Task<C?> GetAsync(K key)
        {
            return await _context.FindAsync<C>(key);
        }

        // UPDATE (NO SaveChanges)
        public async Task<C?> UpdateAsync(K key, C item)
        {
            if (item == null)
                return null;

            var existingItem = await GetAsync(key);

            if (existingItem == null)
                return null;

            _context.Entry(existingItem).CurrentValues.SetValues(item);

            return existingItem;
        }

        //  FIRST OR DEFAULT
        public async Task<C?> FirstOrDefaultAsync(Expression<Func<C, bool>> predicate)
        {
            return await _context.Set<C>().FirstOrDefaultAsync(predicate);
        }

        //  QUERYABLE (IMPORTANT FOR LINQ)
        public IQueryable<C> GetQueryable()
        {
            return _context.Set<C>();
        }

        // PAGINATION / FILTER
        public async Task<IEnumerable<C>> GetAllByForeignKeyAsync(
            Expression<Func<C, bool>> predicate,
            int limit,
            int pageNumber)
        {
            return await _context.Set<C>()
                .Where(predicate)
                .Skip((pageNumber - 1) * limit)
                .Take(limit)
                .ToListAsync();
        }
    }
}
