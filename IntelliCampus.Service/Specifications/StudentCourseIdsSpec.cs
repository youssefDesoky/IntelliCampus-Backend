using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class StudentCourseIdsSpec : BaseSpecifications<StudentCourse>
    {
        public StudentCourseIdsSpec(int studentId)
            : base(sc => sc.StudentId == studentId) { }
    }
}
