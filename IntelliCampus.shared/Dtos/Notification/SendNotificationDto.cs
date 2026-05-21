using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Notification;

public class SendNotificationDto
{
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
}
