using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class AdminSpec : BaseSpecifications<Admin>
    {
        public AdminSpec()
        {
            AddInclude(a => a.Faculty!);
            AddInclude("UserRoles.Role");
            EnableSplitQuery();
        }

        public AdminSpec(AdminQueryParams queryParams)
            : base(a =>
                (string.IsNullOrEmpty(queryParams.Search) || a.FullName.Contains(queryParams.Search)) &&
                (string.IsNullOrEmpty(queryParams.Role) || a.UserRoles.Any(ur => ur.IsActive && ur.Role.RoleName == queryParams.Role)))
        {
            AddInclude(a => a.Faculty!);
            AddInclude("UserRoles.Role");
            EnableSplitQuery();
            AddOrderBy(a => a.FullName);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public AdminSpec(int adminId)
            : base(a => a.UserId == adminId)
        {
            AddInclude(a => a.Faculty!);
            AddInclude("UserRoles.Role");
            EnableSplitQuery();
        }
    }
}
