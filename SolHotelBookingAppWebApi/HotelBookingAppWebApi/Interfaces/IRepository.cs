using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Interfaces
{
    public interface IRepository<K, T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();

        Task<T?> GetByIdAsync(K id);

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task<bool> ExistsAsync(K id);

        Task<T> AddAsync(T entity);

        Task<T?> UpdateAsync(K id, T entity);

        Task<bool> DeleteAsync(K id);
        Task<IEnumerable<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includes);

    }
}
