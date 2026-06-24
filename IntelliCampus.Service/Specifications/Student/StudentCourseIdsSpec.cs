using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal sealed class StudentCourseIdsSpec : BaseSpecifications<StudentCourse>
{
    public StudentCourseIdsSpec(int studentId)
        : base(sc => sc.StudentId == studentId) { }

    public StudentCourseIdsSpec(int studentId, List<StudentCourseStatus> statuses)
        : base(sc => sc.StudentId == studentId && statuses.Contains(sc.Status)) { }

    public StudentCourseIdsSpec(int courseId, bool byCourse)
        : base(sc => sc.CourseId == courseId) { }
}
