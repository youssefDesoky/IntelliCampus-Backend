using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class DeviceTokenByEndpointSpec : BaseSpecifications<DeviceToken>
{
    public DeviceTokenByEndpointSpec(string endpoint)
        : base(dt => dt.Endpoint == endpoint)
    {
    }
}
