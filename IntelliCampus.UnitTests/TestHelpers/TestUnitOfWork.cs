using System.Linq.Expressions;
using IntelliCampus.Domain.Interfaces;

namespace IntelliCampus.UnitTests.TestHelpers;

public class TestUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<(Type, Type), object> _repos = new();
    private Func<Task<int>>? _saveChangesAsync;
    private Func<string, object[], Task>? _executeSqlAsync;
    public int SaveChangesCallCount { get; private set; }

    public void AddRepository<TEntity, TKey>(IGenericRepository<TEntity, TKey> repo) where TEntity : class
    {
        _repos[(typeof(TEntity), typeof(TKey))] = repo;
    }

    public void SetSaveChangesAsync(int result = 1)
    {
        _saveChangesAsync = () => Task.FromResult(result);
    }

    public void SetExecuteSqlAsync()
    {
        _executeSqlAsync = (_, _) => Task.CompletedTask;
    }

    public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class
    {
        if (_repos.TryGetValue((typeof(TEntity), typeof(TKey)), out var repo))
            return (IGenericRepository<TEntity, TKey>)repo;
        throw new InvalidOperationException($"Repository for {typeof(TEntity).Name} not configured.");
    }

    public Task<int> SaveChangesAsync()
    {
        SaveChangesCallCount++;
        if (_saveChangesAsync is not null)
            return _saveChangesAsync();
        throw new InvalidOperationException("SaveChangesAsync not configured.");
    }

    public Task ExecuteSqlAsync(string sql, params object[] parameters)
    {
        if (_executeSqlAsync is not null)
            return _executeSqlAsync(sql, parameters);
        throw new InvalidOperationException("ExecuteSqlAsync not configured.");
    }

    public Task<int> ExecuteUpdateAsync<TEntity, TProperty>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        TProperty value) where TEntity : class
    {
        return Task.FromResult(0);
    }
}
