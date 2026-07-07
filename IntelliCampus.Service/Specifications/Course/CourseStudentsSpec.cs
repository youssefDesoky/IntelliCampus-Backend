using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal sealed class CourseStudentsSpec : BaseSpecifications<StudentCourse>
{
    public CourseStudentsSpec(int courseId)
        : base(sc => sc.CourseId == courseId && sc.Status == StudentCourseStatus.InProgress)
    {
        AddIncludes();
    }

    public CourseStudentsSpec(int courseId, string search)
        : base(sc => sc.CourseId == courseId && sc.Status == StudentCourseStatus.InProgress
            && (sc.Student.User.FullName.Contains(search)
                || (sc.Student.StudentCode != null && sc.Student.StudentCode.Contains(search))
                || (sc.Student.User.FullNameAr != null && sc.Student.User.FullNameAr.Contains(search))))
    {
        AddIncludes();
    }

    private void AddIncludes()
    {
        AddInclude(sc => sc.Student);
        AddInclude(sc => sc.Class!);
        AddInclude("Student.Department");
        AddInclude("Student.Bylaw");
        AddInclude("Student.User.UserRoles.Role");
        EnableSplitQuery();
    }
}
