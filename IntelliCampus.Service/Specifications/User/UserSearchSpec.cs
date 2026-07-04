using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class UserSearchSpec : BaseSpecifications<User>
{
    public UserSearchSpec(int currentUserId, string query)
        : base(u =>
            u.UserId != currentUserId &&
            (
                u.FullName.Contains(query) ||
                u.Email.Contains(query) ||
                u.NationalId.Contains(query) ||
                (u.Student != null && u.Student.StudentCode != null && u.Student.StudentCode.Contains(query)) ||
                u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName != null && ur.Role.RoleName.Contains(query))
            ))
    {
        AddInclude("UserRoles.Role");
        AddInclude(u => u.Student!);
        AddOrderBy(u => u.FullName);
    }
}
