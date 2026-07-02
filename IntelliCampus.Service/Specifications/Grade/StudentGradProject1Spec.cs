using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal class StudentCompletedCoursesSpec : BaseSpecifications<StudentCourse>
{
    public StudentCompletedCoursesSpec(int studentId)
        : base(sc => sc.StudentId == studentId
                    && sc.Status == StudentCourseStatus.Completed)
    {
        AddInclude(sc => sc.Course);
    }
}
