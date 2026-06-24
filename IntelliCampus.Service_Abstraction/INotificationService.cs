using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Notification;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface INotificationService
{
    Task<PaginatedResult<NotificationDto>> GetByUserIdAsync(int userId, NotificationQueryParams queryParams);
    Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId, NotificationQueryParams queryParams);
    Task<NotificationSummaryDto> GetSummaryAsync(int userId, NotificationQueryParams queryParams);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<bool> DeleteAsync(int notificationId, int userId);

    Task SendAsync(int userId, NotificationType type, string message, string? title = null, string? clickUrl = null, string? imageUrl = null);
    Task SendToManyAsync(IEnumerable<int> userIds, NotificationType type, string message, string? title = null, string? clickUrl = null, string? imageUrl = null);
}
