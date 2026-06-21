using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Title { get; set; }
    public string? ClickUrl { get; set; }
    public string? ImageUrl { get; set; }

    // Navigation
    public ICollection<UserNotification> UserNotifications { get; set; } = [];
}
