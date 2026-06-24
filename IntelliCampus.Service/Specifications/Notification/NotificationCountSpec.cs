using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class NotificationCountSpec : BaseSpecifications<UserNotification>
{
    public NotificationCountSpec(int userId, NotificationQueryParams queryParams)
        : base(BuildNotificationExpression(userId, queryParams))
    {
    }

    private static System.Linq.Expressions.Expression<Func<UserNotification, bool>> BuildNotificationExpression(int userId, NotificationQueryParams queryParams)
    {
        NotificationType? parsedType = null;
        if (!string.IsNullOrEmpty(queryParams.Type) && Enum.TryParse<NotificationType>(queryParams.Type, ignoreCase: true, out var nt))
            parsedType = nt;

        return n => n.UserId == userId &&
            (!queryParams.IsRead.HasValue || n.IsRead == queryParams.IsRead.Value) &&
            (!queryParams.DateFrom.HasValue || n.Notification.CreatedAt >= queryParams.DateFrom.Value) &&
            (!queryParams.DateTo.HasValue || n.Notification.CreatedAt <= queryParams.DateTo.Value) &&
            (!parsedType.HasValue || n.Notification.Type == parsedType.Value);
    }
}