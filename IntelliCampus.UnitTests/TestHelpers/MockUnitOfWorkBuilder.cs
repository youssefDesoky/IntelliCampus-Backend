using IntelliCampus.Domain.Interfaces;
using Moq;

namespace IntelliCampus.UnitTests.TestHelpers;

public class MockUnitOfWorkBuilder
{
    private readonly Mock<IUnitOfWork> _mock = new(MockBehavior.Strict);
    private readonly Dictionary<(Type, Type), object> _repos = new();

    public IUnitOfWork Build() => _mock.Object;

    public Mock<IGenericRepository<TEntity, TKey>> GetOrCreateRepo<TEntity, TKey>()
        where TEntity : class
    {
        var key = (typeof(TEntity), typeof(TKey));
        if (_repos.TryGetValue(key, out var existing))
            return (Mock<IGenericRepository<TEntity, TKey>>)existing;

        var repoMock = new Mock<IGenericRepository<TEntity, TKey>>(MockBehavior.Strict);
        _repos[key] = repoMock;
        _mock.Setup(u => u.GetRepository<TEntity, TKey>()).Returns(repoMock.Object);
        return repoMock;
    }

    public MockUnitOfWorkBuilder AddRepository<TEntity, TKey>(Mock<IGenericRepository<TEntity, TKey>> repoMock)
        where TEntity : class
    {
        var key = (typeof(TEntity), typeof(TKey));
        _repos[key] = repoMock;
        _mock.Setup(u => u.GetRepository<TEntity, TKey>()).Returns(repoMock.Object);
        return this;
    }

    public MockUnitOfWorkBuilder SetupSaveChangesAsync(int result = 1)
    {
        _mock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(result);
        return this;
    }

    public MockUnitOfWorkBuilder SetupExecuteSqlAsync()
    {
        _mock.Setup(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns(Task.CompletedTask);
        return this;
    }
}
