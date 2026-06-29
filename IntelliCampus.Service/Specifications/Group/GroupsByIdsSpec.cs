using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class GroupsByIdsSpec : BaseSpecifications<Group>
{
    public GroupsByIdsSpec(List<int> groupIds)
        : base(g => groupIds.Contains(g.GroupId)) { }
}
