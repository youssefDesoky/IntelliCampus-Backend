using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public sealed class ClassByIdSpec : BaseSpecifications<Class>
{
    public ClassByIdSpec(int classId)
        : base(c => c.ClassId == classId)
    {
        AddInclude(c => c.Instructor!);
        AddInclude("Instructor.User");
        AddInclude(c => c.Course!);
        EnableSplitQuery();
    }
}
