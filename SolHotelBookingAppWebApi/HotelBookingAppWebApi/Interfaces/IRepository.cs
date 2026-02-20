using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IRepository<K, T> where T : class
    {
        Task<T?> GetByIdAsync(K id);

        Task<IEnumerable<T>> GetAllAsync();

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task<T> AddAsync(T entity);

        Task<T?> UpdateAsync(T entity);

        Task<T?> DeleteAsync(K id);
    }
}
