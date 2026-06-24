using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Presistence;
using IntelliCampus.Presistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IntelliCmpus.Presistence.Repositories
{
    public class GenericRepository<TEntity, TKey>(IntelliCampusDbContext dbContext) : IGenericRepository<TEntity, TKey>
    where TEntity : class  
    {
        private readonly IntelliCampusDbContext _dbContext = dbContext;

        public void Add(TEntity entity) => _dbContext.Set<TEntity>().Add(entity);
        public void Update(TEntity entity) => _dbContext.Set<TEntity>().Update(entity);
        public void Delete(TEntity entity) => _dbContext.Set<TEntity>().Remove(entity);
        public async Task<IEnumerable<TEntity>> GetAllAsync() 
            => await _dbContext.Set<TEntity>().ToListAsync();
        public async Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity> specifications)
            => await SpecificationBuilder.BuildQuery(_dbContext.Set<TEntity>(), specifications).ToListAsync();
        public async Task<TEntity?> GetByIdAsync(TKey id) 
            => await _dbContext.Set<TEntity>().FindAsync(id);
        public async Task<TEntity?> GetByIdAsync( ISpecifications<TEntity> specifications) 
            => await SpecificationBuilder.BuildQuery(_dbContext.Set<TEntity>(), specifications).FirstOrDefaultAsync();
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbContext.Set<TEntity>().AnyAsync(predicate);

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbContext.Set<TEntity>().CountAsync(predicate);

        public async Task<TResult?> MaxAsync<TResult>(Expression<Func<TEntity, TResult?>> selector)
            => await _dbContext.Set<TEntity>().MaxAsync(selector);

        public void DeleteRange(IEnumerable<TEntity> entities)
            => _dbContext.Set<TEntity>().RemoveRange(entities);

        public async Task<int> CountAsync(ISpecifications<TEntity> specifications)
            => await SpecificationBuilder.BuildQuery(_dbContext.Set<TEntity>(), specifications).CountAsync();
    }
}
