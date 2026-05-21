namespace IntelliCampus.Domain.Entities;

public class UserNotification
{
    public int UserNotificationId { get; set; }
    public int UserId { get; set; }
    public int NotificationId { get; set; }
    public bool IsRead { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Notification Notification { get; set; } = null!;
}
