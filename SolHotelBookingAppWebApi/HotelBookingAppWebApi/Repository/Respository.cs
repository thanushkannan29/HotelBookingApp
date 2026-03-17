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
        public async Task<C?> AddAsync(C entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _context.Set<C>().AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<C?> DeleteAsync(K key)
        {
            var item = await GetAsync(key);
            if (item != null)
            {
                _context.Remove(item);
                await _context.SaveChangesAsync();
                return item;
            }
            return null;
        }

        public async Task<IEnumerable<C>> GetAllAsync()
        {
            return await _context.Set<C>().ToListAsync();
        }

        public async Task<C?> GetAsync(K key)
        {
            var item = await _context.FindAsync<C>(key);
            return item != null ? item : null;
        }

        public async Task<C?> UpdateAsync(K key, C item)
        {
            if (item == null)
                return null;

            var existingItem = await GetAsync(key);
            if (existingItem == null)
                return null;

            _context.Entry(existingItem).CurrentValues.SetValues(item);
            var result = await _context.SaveChangesAsync();

            return result > 0 ? existingItem : null;
        }

        //-------------------------------------------------------------------------//
        public async Task<C?> FirstOrDefaultAsync(Expression<Func<C, bool>> predicate)
        {
            return await _context.Set<C>().FirstOrDefaultAsync(predicate);
        }

        public IQueryable<C> GetQueryable()
        {
            return _context.Set<C>();
        }

        // This is for get details from one table
        public async Task<IEnumerable<C>> GetAllByForeignKeyAsync(Expression<Func<C, bool>> predicate,
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

