using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class ClassSpec : BaseSpecifications<Class>
    {
        public ClassSpec()
        {
            AddInclude(c => c.Course);
            AddInclude(c => c.Instructor);
        }

        public ClassSpec(int classId)
            : base(c => c.ClassId == classId)
        {
            AddInclude(c => c.Course);
            AddInclude(c => c.Instructor);
        }

        public ClassSpec(int courseId, bool byCourse)
            : base(c => c.CourseId == courseId)
        {
            AddInclude(c => c.Course);
            AddInclude(c => c.Instructor);
        }
    }
}
