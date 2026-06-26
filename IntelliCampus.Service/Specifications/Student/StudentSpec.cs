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
            : base(StudentSpecHelper.GetStudentCriteria(queryParams))
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
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
