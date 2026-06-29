using System.Linq.Expressions;

namespace IntelliCampus.Domain.Interfaces
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity> specifications);
        Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity> specifications, bool asNoTracking);
        Task<IEnumerable<TResult>> GetAllAsync<TResult>(
            ISpecifications<TEntity> specifications,
            Expression<Func<TEntity, TResult>> selector,
            bool asNoTracking = false);
        Task<TEntity?> GetByIdAsync(TKey id);
        Task<TEntity?> GetByIdAsync(ISpecifications<TEntity> specifications);
        void Add(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TResult?> MaxAsync<TResult>(Expression<Func<TEntity, TResult?>> selector);
        void DeleteRange(IEnumerable<TEntity> entities);
        Task<int> CountAsync(ISpecifications<TEntity> specifications);

    }
}
