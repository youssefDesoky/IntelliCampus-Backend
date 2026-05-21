namespace IntelliCampus.Shared.Dtos.Notification;

public class NotificationSummaryDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public List<NotificationDto> Recent { get; set; } = [];
}
