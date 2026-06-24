using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class StudentSpec : BaseSpecifications<Student>
    {
        public StudentSpec()
            : base(null)
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");
        }

        public StudentSpec(StudentQueryParams queryParams)
            : base(BuildStudentExpression(queryParams))
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");
        }

        private static System.Linq.Expressions.Expression<Func<Student, bool>> BuildStudentExpression(StudentQueryParams queryParams)
        {
            StudentType? parsedStatus = null;
            if (!string.IsNullOrEmpty(queryParams.Status) && Enum.TryParse<StudentType>(queryParams.Status, ignoreCase: true, out var st))
                parsedStatus = st;

            return s =>
                (!queryParams.DepartmentId.HasValue || s.DepartmentId == queryParams.DepartmentId.Value) &&
                (!queryParams.FacultyId.HasValue || s.FacultyId == queryParams.FacultyId.Value) &&
                (!queryParams.Level.HasValue || s.Level == queryParams.Level.Value) &&
                (string.IsNullOrEmpty(queryParams.Search) || s.FullName.Contains(queryParams.Search)) &&
                (!parsedStatus.HasValue || s.StudentType == parsedStatus.Value);
        }

        public StudentSpec(CourseQueryParams queryParams)
            : base(queryParams.StudentId.HasValue ? s => s.UserId == queryParams.StudentId.Value : null)
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");

            if (queryParams.IncludeCourses)
            {
                AddInclude("StudentCourses.Course.Notes.MaterialFolder");
            }
        }
    }
}
