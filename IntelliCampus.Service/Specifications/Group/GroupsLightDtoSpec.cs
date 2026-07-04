using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class GroupsLightDtoSpec : BaseSpecifications<Group>
{
    public GroupsLightDtoSpec(List<int> groupIds)
        : base(g => groupIds.Contains(g.GroupId))
    {
    }
}
