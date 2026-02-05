using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<UserNotification> UserNotifications { get; set; } = [];
}
