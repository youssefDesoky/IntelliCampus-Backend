using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class ExamWithDetailsSpec : BaseSpecifications<Exam>
{
    public ExamWithDetailsSpec()
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddOrderByDescending(e => e.Date);
    }

    public ExamWithDetailsSpec(int examId)
        : base(e => e.ExamId == examId)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude("Course.StudentCourses");
    }
}
