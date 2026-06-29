using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class StudentCourseSemesterAllSpec : BaseSpecifications<StudentCourse>
{
    public StudentCourseSemesterAllSpec(string semester)
        : base(sc => sc.Semester == semester) { }
}
