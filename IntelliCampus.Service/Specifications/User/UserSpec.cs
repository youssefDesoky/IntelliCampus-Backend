using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class UserSpec : BaseSpecifications<User>
{
    public UserSpec(List<int> userIds)
        : base(u => userIds.Contains(u.UserId)) { }
}
