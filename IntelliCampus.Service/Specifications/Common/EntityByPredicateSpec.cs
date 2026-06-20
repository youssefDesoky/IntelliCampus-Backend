using System.Linq.Expressions;

namespace IntelliCampus.Service.Specifications.Common;

internal class EntityByPredicateSpec<TEntity>(Expression<Func<TEntity, bool>> predicate)
    : BaseSpecifications<TEntity>(predicate)
    where TEntity : class
{
}
