using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class StudentSpec : BaseSpecifications<Student>
    {
        public StudentSpec()
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");
        }

        public StudentSpec(int studentId)
            : base(s => s.UserId == studentId)
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");
        }

        public StudentSpec(int studentId, bool includeCourses)
            : base(s => s.UserId == studentId)
        {
            AddInclude(s => s.Faculty!);
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude(s => s.Specialization!);
            AddInclude("UserRoles.Role");

            if (includeCourses)
            {
                AddInclude("StudentCourses.Course.Notes.MaterialFolder");
            }
        }
    }
}
