using IntelliCampus.Domain.Interfaces;
using System.Linq.Expressions;

namespace IntelliCampus.Service.Specifications
{
    public abstract class BaseSpecifications<TEntity> : ISpecifications<TEntity> where TEntity : class
    {
        protected BaseSpecifications()
        {
            
        }

        #region Criteria
        public Expression<Func<TEntity, bool>>? Criteria { get; }

        protected BaseSpecifications(Expression<Func<TEntity, bool>> criteriaExpression)
        {
            Criteria = criteriaExpression;
        }
        #endregion

        #region Includes
        public ICollection<Expression<Func<TEntity, object>>> IncludeExpressions { get; } = [];

        public List<string> IncludeStrings { get; } = []; //For ThenInclude scenarios

        protected void AddInclude(Expression<Func<TEntity, object>> includeExpression) 
        => IncludeExpressions.Add(includeExpression);

        protected void AddInclude(string includeString)   // for ThenInclude
        => IncludeStrings.Add(includeString);

        #endregion

        #region Sorting
        public Expression<Func<TEntity, object>>? OrderBy { get; private set; }

        public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }

        protected void AddOrderBy(Expression<Func<TEntity, object>> orderByExpression) => OrderBy = orderByExpression;

        protected void AddOrderByDescending(Expression<Func<TEntity, object>> orderByDescendingExpression) => OrderByDescending = orderByDescendingExpression;
        #endregion

        #region Pagination

        public int Take { get; private set; }

        public int Skip { get; private set; }

        public bool IsPaginated { get; private set; }

        public void ApplyPagination(int pageSize, int pageIndex)
        {
            IsPaginated = true;
            Take = pageSize;
            Skip = pageSize * (pageIndex - 1);

        }

        #endregion

        #region Split Query
        public bool UseSplitQuery { get; private set; }

        protected void EnableSplitQuery() => UseSplitQuery = true;
        #endregion

        #region Select
        public Expression? Select { get; private set; }

        protected void AddSelect<TResult>(Expression<Func<TEntity, TResult>> selector) => Select = selector;
        #endregion
    }
}
