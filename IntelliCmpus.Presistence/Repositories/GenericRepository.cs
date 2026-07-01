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

        public async Task<IEnumerable<TResult>> GetAllAsync<TResult>(
            ISpecifications<TEntity> specifications,
            Expression<Func<TEntity, TResult>> selector,
            bool asNoTracking = false)
        {
            var query = SpecificationBuilder.BuildQuery(_dbContext.Set<TEntity>(), specifications);
            if (asNoTracking)
                query = query.AsNoTracking();
            return await query.Select(selector).ToListAsync();
        }

        public void Add(TEntity entity) => _dbContext.Set<TEntity>().Add(entity);
        public void Update(TEntity entity) => _dbContext.Set<TEntity>().Update(entity);
        public void Delete(TEntity entity) => _dbContext.Set<TEntity>().Remove(entity);
        public async Task<IEnumerable<TEntity>> GetAllAsync() 
            => await _dbContext.Set<TEntity>().ToListAsync();
        public async Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity> specifications)
            => await SpecificationBuilder.BuildQuery(_dbContext.Set<TEntity>(), specifications).ToListAsync();
        public async Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<TEntity> specifications, bool asNoTracking)
        {
            var query = SpecificationBuilder.BuildQuery(_dbContext.Set<TEntity>(), specifications);
            if (asNoTracking)
                query = query.AsNoTracking();
            return await query.ToListAsync();
        }
        public async Task<TEntity?> GetByIdAsync(TKey id) 
        {
            if (id is System.Runtime.CompilerServices.ITuple tuple)
            {
                var keyValues = new object?[tuple.Length];
                for (int i = 0; i < tuple.Length; i++)
                    keyValues[i] = tuple[i];
                return await _dbContext.Set<TEntity>().FindAsync(keyValues);
            }
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }
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
