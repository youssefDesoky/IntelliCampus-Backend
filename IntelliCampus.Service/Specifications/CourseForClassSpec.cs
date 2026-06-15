using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class CourseForClassSpec : BaseSpecifications<Course>
    {
        public CourseForClassSpec(int courseId)
            : base(c => c.CourseId == courseId)
        {
            AddInclude(c => c.Department!);
        }
    }
}
