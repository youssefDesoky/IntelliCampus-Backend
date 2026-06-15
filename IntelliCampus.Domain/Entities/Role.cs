namespace IntelliCampus.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;

    public ICollection<UserRoleJunction> UserRoles { get; set; } = [];
}
