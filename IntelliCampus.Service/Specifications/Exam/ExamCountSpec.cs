using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class ExamCountSpec : BaseSpecifications<Exam>
{
    public ExamCountSpec(ExamQueryParams queryParams)
        : base(e =>
            (!queryParams.CourseId.HasValue || e.CourseId == queryParams.CourseId.Value) &&
            (!queryParams.ExamType.HasValue || e.ExamType == queryParams.ExamType.Value) &&
            (!queryParams.Status.HasValue || e.Status == queryParams.Status.Value) &&
            (!queryParams.FacultyId.HasValue || (e.Course.Department != null && e.Course.Department.FacultyId == queryParams.FacultyId.Value)))
    {
    }
}