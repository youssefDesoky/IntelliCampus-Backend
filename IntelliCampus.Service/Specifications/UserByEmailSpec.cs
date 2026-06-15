using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class UserByEmailSpec : BaseSpecifications<User>
    {
        public UserByEmailSpec(string email)
        : base(u => u.Email == email)
        {
            AddInclude("UserRoles.Role");
        }
    }
}
