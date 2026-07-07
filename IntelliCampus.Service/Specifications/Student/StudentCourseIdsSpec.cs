using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal sealed class StudentCourseIdsSpec : BaseSpecifications<StudentCourse>
{
    public StudentCourseIdsSpec(int studentId)
        : base(sc => sc.StudentId == studentId)
    {
        AddInclude(sc => sc.Class!);
    }

    public StudentCourseIdsSpec(int studentId, List<StudentCourseStatus> statuses)
        : base(sc => sc.StudentId == studentId && statuses.Contains(sc.Status)) { }

    public StudentCourseIdsSpec(int courseId, bool byCourse)
        : base(sc => sc.CourseId == courseId) { }

    public StudentCourseIdsSpec(int courseId, bool byCourse, StudentCourseStatus status)
        : base(sc => sc.CourseId == courseId && sc.Status == status) { }

    public StudentCourseIdsSpec(List<int> studentIds, StudentCourseStatus status)
        : base(sc => studentIds.Contains(sc.StudentId) && sc.Status == status) { }

    public StudentCourseIdsSpec(List<string> semesters)
        : base(sc => semesters.Contains(sc.Semester))
    {
        AddInclude("Course.Department");
        AddInclude("Student.User");
    }

    public StudentCourseIdsSpec(int classId, string byClass)
        : base(sc => sc.ClassId == classId) { }
}
