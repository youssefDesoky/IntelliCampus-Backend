using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal sealed class CourseStudentsSpec : BaseSpecifications<StudentCourse>
{
    public CourseStudentsSpec(int courseId)
        : base(sc => sc.CourseId == courseId && sc.Status == StudentCourseStatus.InProgress)
    {
        AddInclude(sc => sc.Student);
        AddInclude(sc => sc.Class!);
        AddInclude("Student.Department");
        AddInclude("Student.Bylaw");
        AddInclude("Student.Specialization");
        AddInclude("Student.User.UserRoles.Role");
        EnableSplitQuery();
    }
}
