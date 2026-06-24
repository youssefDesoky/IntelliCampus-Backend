using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class CourseCountSpec : BaseSpecifications<Course>
    {
        public CourseCountSpec(CourseQueryParams queryParams)
            : base(CourseSpecHelper.GetCourseCriteria(queryParams))
        {
            
        }
    }
}
