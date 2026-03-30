using System.Linq.Expressions;

namespace HotelBookingAppWebApi.Interfaces.RepositoryInterface
{
    /// <summary>Generic repository contract for CRUD and query operations.</summary>
    /// <typeparam name="TKey">Primary key type.</typeparam>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    public interface IRepository<TKey, TEntity> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity?> GetAsync(TKey key);
        Task<TEntity?> AddAsync(TEntity entity);
        Task<TEntity?> DeleteAsync(TKey key);
        Task<TEntity?> UpdateAsync(TKey key, TEntity entity);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
        IQueryable<TEntity> GetQueryable();
        Task<IEnumerable<TEntity>> GetAllByForeignKeyAsync(
            Expression<Func<TEntity, bool>> predicate, int limit, int pageNumber);
    }
}
