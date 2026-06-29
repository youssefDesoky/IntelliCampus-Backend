using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;
using System.Linq.Expressions;

namespace IntelliCampus.Service.Specifications
{
    internal static class CourseSpecHelper
    {
        public static Expression<Func<Course, bool>> GetCourseCriteria(CourseQueryParams queryParams)
        {
            return c =>
                (!queryParams.CourseId.HasValue || c.CourseId == queryParams.CourseId.Value)
                && (!queryParams.DepartmentId.HasValue || c.DepartmentId == queryParams.DepartmentId.Value)
                && (!queryParams.IsActiveOnly || c.Status == CourseStatus.Active)
                && (string.IsNullOrEmpty(queryParams.Search)
                    || c.CourseName.Contains(queryParams.Search)
                    || (c.CourseCode != null && c.CourseCode.Contains(queryParams.Search))
                    || c.Prerequisites.Any(p => p.PrerequisiteCourse != null
                        && (p.PrerequisiteCourse.CourseName.Contains(queryParams.Search)
                            || (p.PrerequisiteCourse.CourseCode != null
                                && p.PrerequisiteCourse.CourseCode.Contains(queryParams.Search)))))
                && (!queryParams.StudentId.HasValue
                    || c.StudentCourses.Any(sc => sc.StudentId == queryParams.StudentId.Value
                        && (queryParams.StudentStatuses == null || queryParams.StudentStatuses.Contains(sc.Status)))
            );
        }
    }
}
