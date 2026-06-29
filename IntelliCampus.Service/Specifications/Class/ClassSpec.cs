using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;
using System.Linq.Expressions;

namespace IntelliCampus.Service.Specifications
{
    internal class ClassSpec : BaseSpecifications<Class>
    {
        public ClassSpec()
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
            EnableSplitQuery();
        }

        public ClassSpec(int classId)
            : base(c => c.ClassId == classId)
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
            EnableSplitQuery();
        }

        public ClassSpec(int courseId, bool byCourse, string? classType = null)
            : base(BuildByCourseExpression(courseId, classType))
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
            EnableSplitQuery();
        }

        public ClassSpec(ClassQueryParams queryParams)
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
            EnableSplitQuery();
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public ClassSpec(int courseId, bool byCourse, ClassQueryParams queryParams)
            : base(BuildByCourseAndClassTypeExpression(courseId, queryParams))
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
            EnableSplitQuery();
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        private static Expression<Func<Class, bool>> BuildByCourseAndClassTypeExpression(int courseId, ClassQueryParams queryParams)
        {
            ClassType? parsedClassType = null;
            if (!string.IsNullOrEmpty(queryParams.ClassType) && Enum.TryParse<ClassType>(queryParams.ClassType, ignoreCase: true, out var ct))
                parsedClassType = ct;

            return c => c.CourseId == courseId &&
                (!parsedClassType.HasValue || c.ClassType == parsedClassType.Value);
        }

        private static Expression<Func<Class, bool>> BuildByCourseExpression(int courseId, string? classType)
        {
            if (string.IsNullOrEmpty(classType))
                return c => c.CourseId == courseId;

            if (Enum.TryParse<ClassType>(classType, ignoreCase: true, out var parsed))
                return c => c.CourseId == courseId && c.ClassType == parsed;

            return c => c.CourseId == courseId;
        }
    }
}
