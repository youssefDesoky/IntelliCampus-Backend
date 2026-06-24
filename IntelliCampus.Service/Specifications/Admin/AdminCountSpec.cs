using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class AdminCountSpec : BaseSpecifications<Admin>
{
    public AdminCountSpec(AdminQueryParams queryParams)
        : base(a =>
            (string.IsNullOrEmpty(queryParams.Search) || a.FullName.Contains(queryParams.Search)) &&
            (string.IsNullOrEmpty(queryParams.Role) || a.UserRoles.Any(ur => ur.IsActive && ur.Role.RoleName == queryParams.Role)))
    {
    }
}