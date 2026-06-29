using IntelliCampus.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence
{
    internal static class SpecificationBuilder
    {
        //Build Query
        public static IQueryable<TEntity> BuildQuery<TEntity>(IQueryable<TEntity> query, ISpecifications<TEntity> specifications) where TEntity : class
        {
            if (specifications is not null)
            {
                if (specifications.Criteria is not null)
                    query = query.Where(specifications.Criteria);

                if (specifications.IncludeExpressions.Any())
                {
                    query = specifications.IncludeExpressions.Aggregate(query,
                        (currentQuery, includeExpression) => currentQuery.Include(includeExpression));
                }

                if(specifications.IncludeStrings.Any())
                {
                    query = specifications.IncludeStrings.Aggregate(query,
                        (currentQuery, includeString) => currentQuery.Include(includeString));
                }

                if (specifications.UseSplitQuery)
                    query = query.AsSplitQuery();

                if (specifications.OrderBy is not null)
                    query = query.OrderBy(specifications.OrderBy);

                if (specifications.OrderByDescending is not null)
                    query = query.OrderByDescending(specifications.OrderByDescending);

                if (specifications.IsPaginated)
                {
                    query = query.Skip(specifications.Skip).Take(specifications.Take);
                }
            }
            return query;
        }

    }
}
