using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

public sealed class ExamWithCourseSpec : BaseSpecifications<Exam>
{
    public ExamWithCourseSpec()
    {
        AddInclude(e => e.Course!);
        AddInclude("Course.StudentCourses");
        EnableSplitQuery();
    }

    public ExamWithCourseSpec(ExamQueryParams queryParams)
        : base(e =>
            (!queryParams.CourseId.HasValue || e.CourseId == queryParams.CourseId.Value) &&
            (!queryParams.ExamType.HasValue || e.ExamType == queryParams.ExamType.Value) &&
            (!queryParams.Status.HasValue || e.Status == queryParams.Status.Value) &&
            (!queryParams.FacultyId.HasValue || (e.Course.Department != null && e.Course.Department.FacultyId == queryParams.FacultyId.Value)))
    {
        AddInclude(e => e.Course!);
        AddInclude("Course.Department");
        AddInclude("Course.StudentCourses");
        EnableSplitQuery();
        AddOrderBy(e => e.ExamId);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
    }

    public ExamWithCourseSpec(int examId)
        : base(e => e.ExamId == examId)
    {
        AddInclude(e => e.Course!);
        AddInclude("Course.StudentCourses");
        EnableSplitQuery();
    }
}
