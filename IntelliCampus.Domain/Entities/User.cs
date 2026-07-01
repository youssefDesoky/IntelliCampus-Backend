namespace IntelliCampus.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Nationality { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string Password { get; set; } = null!;
    public string? ProfileImage { get; set; }
    public int? FacultyId { get; set; }
    public bool MustChangePassword { get; set; }
    public string? RecoveryEmail { get; set; }
    public bool RecoveryEmailVerified { get; set; }

    // Navigation properties
    public Student? Student { get; set; }
    public Instructor? Instructor { get; set; }
    public Admin? Admin { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<UserRoleJunction> UserRoles { get; set; } = [];
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public UserNotificationSettings? NotificationSettings { get; set; }
    public ICollection<UserNotification> UserNotifications { get; set; } = [];
    public ICollection<GroupMember> GroupMembers { get; set; } = [];
    public ICollection<PostVote> PostVotes { get; set; } = [];
}
