using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
internal class CoursePrerequisiteWithCourseSpec : BaseSpecifications<CoursePrerequisite>
{
    public CoursePrerequisiteWithCourseSpec() 
    {
        AddInclude(cp => cp.PrerequisiteCourse);
    }

    public CoursePrerequisiteWithCourseSpec(int courseId)
        : base(cp => cp.CourseId == courseId)
    {
    }

    public CoursePrerequisiteWithCourseSpec(int courseId, bool byPrerequisiteCourseId)
        : base(cp => cp.PrerequisiteCourseId == courseId)
    {
    }
}
}
