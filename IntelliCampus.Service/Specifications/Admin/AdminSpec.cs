using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class AdminSpec : BaseSpecifications<Admin>
    {
        public AdminSpec()
        {
            AddInclude(a => a.User.Faculty!);
            AddInclude("User.UserRoles.Role");
            EnableSplitQuery();
        }

        public AdminSpec(AdminQueryParams queryParams)
            : base(a =>
                (string.IsNullOrEmpty(queryParams.Search) || a.User.FullName.Contains(queryParams.Search)) &&
                (string.IsNullOrEmpty(queryParams.Role) || a.User.UserRoles.Any(ur => ur.IsActive && ur.Role.RoleName == queryParams.Role)) &&
                (!queryParams.FacultyId.HasValue || a.User.FacultyId == queryParams.FacultyId.Value))
        {
            AddInclude(a => a.User.Faculty!);
            AddInclude("User.UserRoles.Role");
            EnableSplitQuery();
            AddOrderBy(a => a.User.FullName);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public AdminSpec(int adminId)
            : base(a => a.UserId == adminId)
        {
            AddInclude(a => a.User.Faculty!);
            AddInclude("User.UserRoles.Role");
            EnableSplitQuery();
        }
    }
}
