using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class BylawCourseSpec : BaseSpecifications<BylawCourse>
{
    public BylawCourseSpec()
    {
        AddInclude(bc => bc.Course);
        AddInclude(bc => bc.Prerequisites);
        AddInclude(bc => bc.PrerequisiteFor);
    }

    public BylawCourseSpec(int bylawCourseId)
        : base(bc => bc.BylawCourseId == bylawCourseId)
    {
        AddInclude(bc => bc.Course);
        AddInclude(bc => bc.Prerequisites);
        AddInclude(bc => bc.PrerequisiteFor);
    }

    public BylawCourseSpec(int bylawId, int courseId)
        : base(bc => bc.BylawId == bylawId && bc.CourseId == courseId)
    {
        AddInclude(bc => bc.Course);
    }
}
