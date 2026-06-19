using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class ExamWithCourseSpec : BaseSpecifications<Exam>
{
    public ExamWithCourseSpec(int examId)
        : base(e => e.ExamId == examId)
    {
        AddInclude(e => e.Course!);
        AddInclude("Course.StudentCourses");
    }
}
