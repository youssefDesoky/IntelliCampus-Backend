using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class ClassByCourseSpec : BaseSpecifications<Class>
{
    public ClassByCourseSpec(int courseId)
        : base(c => c.CourseId == courseId)
    {
        AddInclude(c => c.Room!);
    }

    public ClassByCourseSpec(int classId, int courseId)
        : base(c => c.ClassId == classId && c.CourseId == courseId)
    {
        AddInclude(c => c.Room!);
    }
}
