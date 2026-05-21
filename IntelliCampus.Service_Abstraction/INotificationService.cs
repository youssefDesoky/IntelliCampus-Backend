using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Notification;

namespace IntelliCampus.Service_Abstraction;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId);
    Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId);
    Task<NotificationSummaryDto> GetSummaryAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<bool> DeleteAsync(int notificationId, int userId);

    Task SendAsync(int userId, NotificationType type, string message);
    Task SendToManyAsync(IEnumerable<int> userIds, NotificationType type, string message);
}
