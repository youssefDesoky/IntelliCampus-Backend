using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public sealed class ExamWithDetailsSpec : BaseSpecifications<Exam>
{
    public ExamWithDetailsSpec()
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        EnableSplitQuery();
        AddOrderByDescending(e => e.Date);
    }

    public ExamWithDetailsSpec(int examId)
        : base(e => e.ExamId == examId)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        AddInclude("Course.StudentCourses");
        EnableSplitQuery();
    }

    public ExamWithDetailsSpec(ExamQueryParams queryParams)
        : base(e =>
            (!queryParams.CourseId.HasValue || e.CourseId == queryParams.CourseId.Value) &&
            (!queryParams.ExamType.HasValue || e.ExamType == queryParams.ExamType.Value) &&
            (!queryParams.Status.HasValue || e.Status == queryParams.Status.Value))
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        EnableSplitQuery();
        AddOrderByDescending(e => e.Date);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public ExamWithDetailsSpec(int courseId, bool filterByCourse)
        : base(e => e.CourseId == courseId)
    {
        AddInclude(e => e.Course!);
        AddInclude(e => e.Room!);
        EnableSplitQuery();
        AddOrderByDescending(e => e.Date);
    }
}
