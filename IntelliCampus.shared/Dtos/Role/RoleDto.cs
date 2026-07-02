namespace IntelliCampus.Shared.Dtos.Role;

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
}

public class UserRoleDto
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class AssignRoleDto
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
}

public class UpdateUserRoleDto
{
    public bool IsActive { get; set; }
}
