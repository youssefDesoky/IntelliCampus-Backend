using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class StudentCourseSemesterSpec : BaseSpecifications<StudentCourse>
{
    public StudentCourseSemesterSpec(int studentId, string semester)
        : base(sc => sc.StudentId == studentId && sc.Semester == semester)
    {
        AddInclude(sc => sc.Course);
        AddInclude(sc => sc.Class!);
        EnableSplitQuery();
    }
}
