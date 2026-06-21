using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class DeviceTokenSpec : BaseSpecifications<DeviceToken>
{
    public DeviceTokenSpec(IEnumerable<int> userIds)
        : base(dt => dt.IsActive && userIds.Contains(dt.UserId))
    {
    }
}
