using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? PostId { get; set; }

    // Navigation properties
    public Post? Post { get; set; }
    public ICollection<UserNotification> UserNotifications { get; set; } = [];
}
