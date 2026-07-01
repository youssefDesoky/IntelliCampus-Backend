using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class BylawCourseSpec : BaseSpecifications<BylawCourse>
{
    public BylawCourseSpec()
    {
        AddInclude(bc => bc.Course);
        AddInclude(bc => bc.Prerequisites);
        AddInclude(bc => bc.PrerequisiteFor);
        EnableSplitQuery();
    }

    public BylawCourseSpec(int bylawCourseId)
        : base(bc => bc.BylawCourseId == bylawCourseId)
    {
        AddInclude(bc => bc.Course);
        AddInclude(bc => bc.Prerequisites);
        AddInclude(bc => bc.PrerequisiteFor);
        EnableSplitQuery();
    }

    public BylawCourseSpec(int bylawId, int courseId)
        : base(bc => bc.BylawId == bylawId && bc.CourseId == courseId)
    {
        AddInclude(bc => bc.Course);
    }

    public BylawCourseSpec(int bylawId, bool _ = false)
        : base(bc => bc.BylawId == bylawId) { }

    public BylawCourseSpec(int courseId, bool byCourseId, bool _ = false)
        : base(bc => bc.CourseId == courseId) { }

    // Batch load by BylawCourseId list (with Course include)
    public BylawCourseSpec(List<int> bylawCourseIds, bool byPk)
        : base(bc => bylawCourseIds.Contains(bc.BylawCourseId))
    {
        AddInclude(bc => bc.Course);
    }

    // Batch load by BylawId list (no includes)
    public BylawCourseSpec(List<int> bylawIds, bool byBylawId, bool noIncludes)
        : base(bc => bylawIds.Contains(bc.BylawId)) { }
}
