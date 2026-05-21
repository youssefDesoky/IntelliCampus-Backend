using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class NotificationSpec : BaseSpecifications<UserNotification>
{
    public NotificationSpec(int userId)
        : base(n => n.UserId == userId)
    {
        AddInclude(n => n.Notification);
        AddOrderByDescending(n => n.Notification.CreatedAt);
    }

    public NotificationSpec(int userId, bool unreadOnly)
        : base(n => n.UserId == userId && !n.IsRead)
    {
        AddInclude(n => n.Notification);
        AddOrderByDescending(n => n.Notification.CreatedAt);
    }

    public NotificationSpec(int userId, int notificationId)
        : base(n => n.UserId == userId && n.NotificationId == notificationId) { }

    public NotificationSpec(int userId, bool unreadOnly, bool forUpdate)
        : base(n => n.UserId == userId && !n.IsRead) { }
}
