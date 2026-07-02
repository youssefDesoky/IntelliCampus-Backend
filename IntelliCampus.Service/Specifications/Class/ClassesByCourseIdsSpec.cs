using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

internal class ClassesByCourseIdsSpec : BaseSpecifications<Class>
{
    public ClassesByCourseIdsSpec(List<int> courseIds, ClassType classType)
        : base(c => courseIds.Contains(c.CourseId) && c.ClassType == classType) { }
}
