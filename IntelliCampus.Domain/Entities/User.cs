using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public abstract class User
{
    public int UserId { get; set; }
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Nationality { get; set; }
    public UserRole Role { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string Password { get; set; } = null!;
    public string? ProfileImage { get; set; }

    // Navigation properties
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<UserNotification> UserNotifications { get; set; } = [];
}
