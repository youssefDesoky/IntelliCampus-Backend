using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class UserRoleJunctionSpec : BaseSpecifications<UserRoleJunction>
    {
        public UserRoleJunctionSpec(int userId)
            : base(ur => ur.UserId == userId)
        {
            AddInclude(ur => ur.Role!);
        }
    }
}
