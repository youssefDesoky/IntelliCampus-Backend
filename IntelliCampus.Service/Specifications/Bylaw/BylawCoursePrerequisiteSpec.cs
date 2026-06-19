using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class BylawCoursePrerequisiteSpec : BaseSpecifications<BylawCoursePrerequisite>
{
    public BylawCoursePrerequisiteSpec()
    {
        AddInclude(bcp => bcp.PrerequisiteCourse.Course);
    }

    public BylawCoursePrerequisiteSpec(int bylawCourseId)
        : base(bcp => bcp.BylawCourseId == bylawCourseId)
    {
        AddInclude(bcp => bcp.PrerequisiteCourse.Course);
    }

    public BylawCoursePrerequisiteSpec(int bylawCourseId, bool isPrerequisiteFor)
        : base(bcp => isPrerequisiteFor
            ? bcp.PrerequisiteBylawCourseId == bylawCourseId
            : bcp.BylawCourseId == bylawCourseId)
    {
        AddInclude(bcp => bcp.PrerequisiteCourse.Course);
    }
}
