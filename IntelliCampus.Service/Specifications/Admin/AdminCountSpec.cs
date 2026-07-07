using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class AdminCountSpec : BaseSpecifications<Admin>
{
    public AdminCountSpec(AdminQueryParams queryParams)
        : base(a =>
            (string.IsNullOrEmpty(queryParams.Search) || a.User.FullName.Contains(queryParams.Search)) &&
            (string.IsNullOrEmpty(queryParams.Role) || a.User.UserRoles.Any(ur => ur.IsActive && ur.Role.RoleName == queryParams.Role)) &&
            (!queryParams.FacultyId.HasValue || a.User.FacultyId == queryParams.FacultyId.Value))
    {
    }
}