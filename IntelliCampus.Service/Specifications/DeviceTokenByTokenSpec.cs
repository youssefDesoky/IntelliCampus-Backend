using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class DeviceTokenByTokenSpec : BaseSpecifications<DeviceToken>
{
    public DeviceTokenByTokenSpec(string token)
        : base(dt => dt.Token == token)
    {
    }
}
