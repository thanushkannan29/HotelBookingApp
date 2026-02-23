using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Repository
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        protected readonly HotelBookingContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(HotelBookingContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // GET ALL
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        // GET BY ID
        public async Task<T?> GetByIdAsync(K id)
        {
            return await _dbSet.FindAsync(id);
        }

        // FIND (WHERE)
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // EXISTS
        public async Task<bool> ExistsAsync(K id)
        {
            var entity = await _dbSet.FindAsync(id);
            return entity != null;
        }

        // ADD
        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // UPDATE
        public async Task<T?> UpdateAsync(K id, T entity)
        {
            var existing = await _dbSet.FindAsync(id);

            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(entity);

            await _context.SaveChangesAsync();
            return existing;
        }

        // DELETE
        public async Task<bool> DeleteAsync(K id)
        {
            var entity = await _dbSet.FindAsync(id);

            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        //this help for var reservations = await repo.GetAllWithIncludeAsync(r => r.User, r => r.ReservationRooms);

        public async Task<IEnumerable<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
                query = query.Include(include);

            return await query.ToListAsync();
        }

    }
}
