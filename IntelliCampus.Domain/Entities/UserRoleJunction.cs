namespace IntelliCampus.Domain.Entities;

public class UserRoleJunction
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
