using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class StudentCourseWithStudentSpec : BaseSpecifications<StudentCourse>
    {
        public StudentCourseWithStudentSpec()
        {
            AddInclude(sc => sc.Student);
            AddInclude("Student.User");
        }
    }
}
