using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class StudentCourseIdsSpec : BaseSpecifications<StudentCourse>
{
    public StudentCourseIdsSpec(int studentId)
        : base(sc => sc.StudentId == studentId) { }

    public StudentCourseIdsSpec(int courseId, bool byCourse)
        : base(sc => sc.CourseId == courseId) { }
}
