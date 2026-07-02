using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class UsersByIdsSpec : BaseSpecifications<User>
{
    public UsersByIdsSpec(List<int> userIds)
        : base(u => userIds.Contains(u.UserId))
    {
        AddInclude("UserRoles.Role");
    }
}
