using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Notification;

namespace IntelliCampus.Service;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationStreamService _notificationStreamService;

    public NotificationService(IUnitOfWork unitOfWork, INotificationStreamService notificationStreamService)
    {
        _unitOfWork = unitOfWork;
        _notificationStreamService = notificationStreamService;
    }

    private IGenericRepository<Notification, int> Notifications
        => _unitOfWork.GetRepository<Notification, int>();

    private IGenericRepository<UserNotification, int> UserNotifications
        => _unitOfWork.GetRepository<UserNotification, int>();

    public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId)
    {
        var spec = new NotificationSpec(userId);
        var userNotifications = await UserNotifications.GetAllAsync(spec);
        return userNotifications.Select(MapToDto);
    }

    public async Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId)
    {
        var spec = new NotificationSpec(userId, unreadOnly: true);
        var userNotifications = await UserNotifications.GetAllAsync(spec);
        return userNotifications.Select(MapToDto);
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(int userId)
    {
        var spec = new NotificationSpec(userId);
        var all = (await UserNotifications.GetAllAsync(spec)).ToList();

        return new NotificationSummaryDto
        {
            TotalCount = all.Count,
            UnreadCount = all.Count(n => !n.IsRead),
            Recent = all.Take(5).Select(MapToDto).ToList()
        };
    }

    public async Task<int> GetUnreadCountAsync(int userId)
        => await UserNotifications.CountAsync(
            n => n.UserId == userId && !n.IsRead);

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var spec = new NotificationSpec(userId, notificationId);
        var userNotification = await UserNotifications.GetByIdAsync(spec);

        if (userNotification is null) return false;

        userNotification.IsRead = true;
        UserNotifications.Update(userNotification);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var spec = new NotificationSpec(userId, unreadOnly: true, forUpdate: true);
        var unread = await UserNotifications.GetAllAsync(spec);

        foreach (var item in unread)
        {
            item.IsRead = true;
            UserNotifications.Update(item);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int notificationId, int userId)
    {
        var spec = new NotificationSpec(userId, notificationId);
        var userNotification = await UserNotifications.GetByIdAsync(spec);

        if (userNotification is null) return false;

        UserNotifications.Delete(userNotification);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task SendAsync(int userId, NotificationType type, string message)
    {
        var notification = new Notification
        {
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };

        Notifications.Add(notification);
        await _unitOfWork.SaveChangesAsync();

        var userNotification = new UserNotification
        {
            UserId = userId,
            NotificationId = notification.NotificationId,
            IsRead = false,
            Notification = notification
        };

        UserNotifications.Add(userNotification);

        await _unitOfWork.SaveChangesAsync();

        _notificationStreamService.Publish(userId, MapToDto(userNotification));
    }

    public async Task SendToManyAsync(
        IEnumerable<int> userIds,
        NotificationType type,
        string message)
    {
        var userIdList = userIds.ToList();
        if (userIdList.Count == 0) return;

        var notification = new Notification
        {
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };

        Notifications.Add(notification);
        await _unitOfWork.SaveChangesAsync();

        var createdUserNotifications = new List<UserNotification>();

        foreach (var userId in userIdList)
        {
            var userNotification = new UserNotification
            {
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                Notification = notification
            };

            UserNotifications.Add(userNotification);
            createdUserNotifications.Add(userNotification);
        }

        await _unitOfWork.SaveChangesAsync();

        foreach (var userNotification in createdUserNotifications)
        {
            _notificationStreamService.Publish(userNotification.UserId, MapToDto(userNotification));
        }
    }

    private static NotificationDto MapToDto(UserNotification un) => new()
    {
        NotificationId = un.NotificationId,
        UserNotificationId = un.UserNotificationId,
        Message = un.Notification.Message,
        Type = un.Notification.Type,
        TypeLabel = GetTypeLabel(un.Notification.Type),
        IsRead = un.IsRead,
        CreatedAt = un.Notification.CreatedAt.ToString("dd MM yy HH:mm:ss"),
        TimeAgo = GetTimeAgo(un.Notification.CreatedAt)
    };

    private static string GetTypeLabel(NotificationType type) => type switch
    {
        NotificationType.CourseRegistered => "Course Registration",
        NotificationType.AssignmentSubmitted => "Assignment Submitted",
        NotificationType.AssignmentGraded => "Assignment Graded",
        NotificationType.NewAssignmentPosted => "New Assignment",
        NotificationType.QuizSubmitted => "Quiz Submitted",
        NotificationType.QuizGraded => "Quiz Graded",
        NotificationType.NewQuizPosted => "New Quiz",
        NotificationType.AttendanceWarning => "Attendance Warning",
        NotificationType.ScheduleUpdated => "Schedule Updated",
        NotificationType.ClassCancelled => "Class Cancelled",
        NotificationType.GradeComplaintReviewed => "Complaint Reviewed",
        NotificationType.MaterialUploaded => "Material Uploaded",
        NotificationType.Announcement => "Announcement",
        NotificationType.Reminder => "Reminder",
        _ => "Notification"
    };

    private static string GetTimeAgo(DateTime createdAt)
    {
        var diff = DateTime.UtcNow - createdAt;

        return diff.TotalMinutes < 1 ? "Just now"
             : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes} minutes ago"
             : diff.TotalHours < 24 ? $"{(int)diff.TotalHours} hours ago"
             : diff.TotalDays < 7 ? $"{(int)diff.TotalDays} days ago"
             : diff.TotalDays < 30 ? $"{(int)(diff.TotalDays / 7)} weeks ago"
             : diff.TotalDays < 365 ? $"{(int)(diff.TotalDays / 30)} months ago"
             : $"{(int)(diff.TotalDays / 365)} years ago";
    }
}