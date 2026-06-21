using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Notification;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public int UserNotificationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ClickUrl { get; set; }
    public string? ImageUrl { get; set; }
}
