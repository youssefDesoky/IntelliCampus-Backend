using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class NotificationSpec : BaseSpecifications<UserNotification>
{
    public static System.Linq.Expressions.Expression<Func<UserNotification, bool>> BuildFilterExpression(int userId, NotificationQueryParams queryParams)
        => BuildNotificationExpression(userId, queryParams);

    public NotificationSpec(int userId)
        : base(n => n.UserId == userId)
    {
        AddInclude(n => n.Notification!);
        AddOrderByDescending(n => n.Notification.CreatedAt);
    }

    public NotificationSpec(int userId, bool unreadOnly)
        : base(n => n.UserId == userId && !n.IsRead)
    {
        AddInclude(n => n.Notification!);
        AddOrderByDescending(n => n.Notification.CreatedAt);
    }

    public NotificationSpec(int userId, NotificationQueryParams queryParams)
        : base(BuildNotificationExpression(userId, queryParams))
    {
        AddInclude(n => n.Notification!);
        AddOrderByDescending(n => n.Notification.CreatedAt);
        ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
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

    public NotificationSpec(int userId, int notificationId)
        : base(n => n.UserId == userId && n.UserNotificationId == notificationId)
    {
        AddInclude(n => n.Notification!);
    }

    public NotificationSpec(int userId, bool unreadOnly, bool forUpdate)
        : base(n => n.UserId == userId && !n.IsRead) { }
}
