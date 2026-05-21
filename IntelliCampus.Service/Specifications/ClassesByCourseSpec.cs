using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class ClassesByCourseSpec : BaseSpecifications<Class>
{
    public ClassesByCourseSpec(int courseId)
        : base(c => c.CourseId == courseId) { }
}
