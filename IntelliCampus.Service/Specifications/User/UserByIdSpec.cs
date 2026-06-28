using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class UserByIdSpec : BaseSpecifications<User>
    {
        public UserByIdSpec(int userId)
        : base(u => u.UserId == userId)
        {
            AddInclude(i => i.Faculty!);
            AddInclude("UserRoles.Role");
        }
    }
}
