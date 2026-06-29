using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class GroupMembersByGroupIdsSpec : BaseSpecifications<GroupMember>
{
    public GroupMembersByGroupIdsSpec(List<int> groupIds)
        : base(gm => groupIds.Contains(gm.GroupId)) { }
}
