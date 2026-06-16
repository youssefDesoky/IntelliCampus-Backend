using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Presistence.Data.Contexts;
using IntelliCmpus.Presistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Repositories
{
    public class UnitOfWork(IntelliCampusDbContext dbContext) : IUnitOfWork
    {
        private readonly IntelliCampusDbContext _dbContext = dbContext;
        private readonly Dictionary<Type, object> _repositories = [];

        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class
        {
            var entityType = typeof(TEntity);
            if (_repositories.TryGetValue(entityType, out object? repository))
                return (IGenericRepository<TEntity, TKey>) repository;

            var newRepository = new GenericRepository<TEntity, TKey>(_dbContext);
            _repositories.Add(entityType, newRepository);
            return newRepository;
        }

        public async Task<int> SaveChangesAsync() => await _dbContext.SaveChangesAsync();

        public async Task ExecuteSqlAsync(string sql, params object[] parameters)
            => await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
    }
}
