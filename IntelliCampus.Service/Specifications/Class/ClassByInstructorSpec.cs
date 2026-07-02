using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class ClassByInstructorSpec : BaseSpecifications<Class>
{
    public ClassByInstructorSpec(int instructorId)
        : base(c => c.InstructorId == instructorId)
    {
        AddInclude(c => c.Course!);
    }
}
