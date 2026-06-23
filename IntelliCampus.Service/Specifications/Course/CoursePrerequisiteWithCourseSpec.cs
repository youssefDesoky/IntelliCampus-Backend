using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class CoursePrerequisiteWithCourseSpec : BaseSpecifications<CoursePrerequisite>
    {
        public CoursePrerequisiteWithCourseSpec()
        {
            AddInclude(cp => cp.PrerequisiteCourse);
        }
    }
}
