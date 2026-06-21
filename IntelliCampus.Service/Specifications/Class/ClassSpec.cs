using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using System.Linq.Expressions;

namespace IntelliCampus.Service.Specifications
{
    internal class ClassSpec : BaseSpecifications<Class>
    {
        public ClassSpec()
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
        }

        public ClassSpec(int classId)
            : base(c => c.ClassId == classId)
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
        }

        public ClassSpec(int courseId, bool byCourse, string? classType = null)
            : base(BuildByCourseExpression(courseId, classType))
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
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
