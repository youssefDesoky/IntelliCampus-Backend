using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Notification;

public class SendBulkNotificationDto
{
    public List<int> UserIds { get; set; } = [];
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? Title { get; set; }
    public string? ClickUrl { get; set; }
    public string? ImageUrl { get; set; }
}
