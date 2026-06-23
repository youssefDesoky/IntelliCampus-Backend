using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class CourseRouterSpec : BaseSpecifications<Course>
    {
        public CourseRouterSpec()
        {
            AddInclude("Prerequisites.PrerequisiteCourse");
        }
    }
}
