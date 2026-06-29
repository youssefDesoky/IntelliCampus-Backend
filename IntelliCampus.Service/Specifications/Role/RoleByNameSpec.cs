using IntelliCampus.Domain.Entities;
using IntelliCampus.Service.Specifications;

namespace IntelliCampus.Service.Specifications;

internal sealed class RoleByNameSpec : BaseSpecifications<Role>
{
    public RoleByNameSpec(string roleName)
        : base(r => r.RoleName == roleName) { }
}
