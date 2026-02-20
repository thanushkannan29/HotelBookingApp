using HotelBookingAppWebApi.Contexts;
using HotelBookingAppWebApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAppWebApi.Repository
{
    public class Respository<K, T> : IRepository<K, T> where T : class
    {
        private readonly HotelBookingContext _context;

        public Respository(HotelBookingContext context)
        {
            _context = context;
        }

        public async Task<T?> AddAsync(T entity)
        {
            _context.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> DeleteAsync(K id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with id {id} not found.");
            }
            _context.Remove(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<T>?> GetAllAsync()
        {
            var entities = _context.Set<T>();
            if (entities == null)
            {
                throw new InvalidOperationException("No entities found.");
            }
            return await entities.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(K id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with id {id} not found.");
            }
            return entity;
        }

        public async Task<T?> UpdateAsync(K key, T entity)
        {
            var entry = await GetByIdAsync(key);
            if (entry == null)
            {
                throw new KeyNotFoundException($"Entity with id {key} not found.");
            }
            _context.Entry(entry).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync();
            return entry;
        }
    }
}
