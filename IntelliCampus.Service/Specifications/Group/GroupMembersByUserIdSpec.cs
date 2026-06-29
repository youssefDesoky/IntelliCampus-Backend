using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class GroupMembersByUserIdSpec : BaseSpecifications<GroupMember>
{
    public GroupMembersByUserIdSpec(int userId)
        : base(gm => gm.UserId == userId) { }
}
