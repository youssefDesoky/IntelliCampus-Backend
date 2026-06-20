using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class AdminSpec : BaseSpecifications<Admin>
    {
        public AdminSpec()
        {
            AddInclude(a => a.Faculty!);
            AddInclude("UserRoles.Role");
        }

        public AdminSpec(int adminId)
            : base(a => a.UserId == adminId)
        {
            AddInclude(a => a.Faculty!);
            AddInclude("UserRoles.Role");
        }
    }
}
