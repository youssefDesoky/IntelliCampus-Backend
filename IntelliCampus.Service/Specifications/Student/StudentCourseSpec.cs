using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class StudentCourseSpec : BaseSpecifications<StudentCourse>
{
    // GetStudentRegistrationsAsync
    public StudentCourseSpec(int studentId)
        : base(sc => sc.StudentId == studentId)
    {
        AddInclude(sc => sc.Class!);
        AddInclude("Class.Instructor.User");
        AddInclude("Class.Room");
        AddInclude("Course.Classes.Instructor");
        AddInclude("Course.Classes.Instructor.User");
        EnableSplitQuery();
    }

    // Check existing + Unregister
    public StudentCourseSpec(int studentId, int courseId)
        : base(sc => sc.StudentId == studentId && sc.CourseId == courseId) { }
}
