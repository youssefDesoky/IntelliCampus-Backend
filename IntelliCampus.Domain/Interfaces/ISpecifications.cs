using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IntelliCampus.Domain.Interfaces
{
    public interface ISpecifications<TEntity> where TEntity : class
    {
        public ICollection<Expression<Func<TEntity, object>>> IncludeExpressions { get; }
        List<string> IncludeStrings { get; }
        public Expression<Func<TEntity, bool>>? Criteria { get; }
        public Expression<Func<TEntity, object>>? OrderBy { get; }
        public Expression<Func<TEntity, object>>? OrderByDescending { get; }

        
        public int Take { get; }
        public int Skip { get; }
        public bool IsPaginated { get; }

        public bool UseSplitQuery { get; }
        public Expression? Select { get; }
    }
}
