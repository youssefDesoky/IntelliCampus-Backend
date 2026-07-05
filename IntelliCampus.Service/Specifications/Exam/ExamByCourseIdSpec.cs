using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class ExamByCourseIdSpec : BaseSpecifications<Exam>
{
    public ExamByCourseIdSpec(int courseId)
        : base(e => e.CourseId == courseId)
    {
    }
}
