using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class CourseByCodeSpec : BaseSpecifications<Course>
{
    public CourseByCodeSpec(string courseCode)
        : base(c => c.CourseCode == courseCode)
    {
    }
}
