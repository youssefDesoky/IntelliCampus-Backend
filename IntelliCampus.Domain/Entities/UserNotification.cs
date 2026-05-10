namespace IntelliCampus.Domain.Entities;

public class UserNotification
{
    public int UserId { get; set; }
    public int NotificationId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Notification Notification { get; set; } = null!;
}
