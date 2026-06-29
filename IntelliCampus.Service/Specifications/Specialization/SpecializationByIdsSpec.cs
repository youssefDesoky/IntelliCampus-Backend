using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class SpecializationByIdsSpec : BaseSpecifications<Specialization>
{
    public SpecializationByIdsSpec(List<int> specializationIds)
        : base(s => specializationIds.Contains(s.SpecializationId)) { }
}
