using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class SpecializationPrerequisiteSpec : BaseSpecifications<SpecializationPrerequisite>
{
    public SpecializationPrerequisiteSpec(int specializationId)
        : base(sp => sp.SpecializationId == specializationId)
    {
        AddInclude(sp => sp.Course);
    }
}
