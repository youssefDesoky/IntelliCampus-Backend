using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class CourseBasicSpec : BaseSpecifications<Course>
{
    // No filter — includes Department for admin dashboard faculty-scoping
    public CourseBasicSpec() : base(null)
    {
        AddInclude(c => c.Department!);
    }

    public CourseBasicSpec(List<int> courseIds)
        : base(c => courseIds.Contains(c.CourseId)) { }

    public CourseBasicSpec(List<string> courseCodes, bool byCodes)
        : base(c => courseCodes.Contains(c.CourseCode!)) { }
}
