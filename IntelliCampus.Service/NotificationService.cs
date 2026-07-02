using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Notification;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationStreamService _notificationStreamService;
    private readonly IPushSender _pushSender;

    public NotificationService(IUnitOfWork unitOfWork, INotificationStreamService notificationStreamService, IPushSender pushSender)
    {
        _unitOfWork = unitOfWork;
        _notificationStreamService = notificationStreamService;
        _pushSender = pushSender;
    }

    private IGenericRepository<Notification, int> Notifications
        => _unitOfWork.GetRepository<Notification, int>();

    private IGenericRepository<UserNotification, int> UserNotifications
        => _unitOfWork.GetRepository<UserNotification, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<PaginatedResult<NotificationDto>> GetByUserIdAsync(int userId, NotificationQueryParams queryParams)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var spec = new NotificationSpec(userId, queryParams);
        var userNotifications = await UserNotifications.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = userNotifications.Select(MapToDto).ToList();

        var countSpec = new NotificationCountSpec(userId, queryParams);
        var totalCount = await UserNotifications.CountAsync(countSpec);

        return new PaginatedResult<NotificationDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<IEnumerable<NotificationDto>> GetUnreadAsync(int userId, NotificationQueryParams queryParams)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var spec = new NotificationSpec(userId, queryParams);
        var userNotifications = await UserNotifications.GetAllAsync(spec, asNoTracking: true);
        return userNotifications.Where(n => !n.IsRead).Select(MapToDto);
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(int userId, NotificationQueryParams queryParams)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var filterExpr = NotificationSpec.BuildFilterExpression(userId, queryParams);
        var totalCount = await UserNotifications.CountAsync(filterExpr);
        var unreadCount = await UserNotifications.CountAsync(n =>
            n.UserId == userId && !n.IsRead);

        var recentSpec = new NotificationSpec(userId, queryParams);
        var recent = (await UserNotifications.GetAllAsync(recentSpec, asNoTracking: true))
            .Take(5).Select(MapToDto).ToList();

        return new NotificationSummaryDto
        {
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Recent = recent
        };
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        return await UserNotifications.CountAsync(
            n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var spec = new NotificationSpec(userId, notificationId);
        var userNotification = await UserNotifications.GetByIdAsync(spec);

        if (userNotification is null) throw new NotificationNotFoundException($"Notification with ID {notificationId} not found for user {userId}.");

        userNotification.IsRead = true;
        UserNotifications.Update(userNotification);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var unreadNotifications = (await UserNotifications.GetAllAsync(
            new NotificationSpec(userId, unreadOnly: true, forUpdate: true),
            asNoTracking: false)).ToList();

        foreach (var un in unreadNotifications)
            un.IsRead = true;

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int notificationId, int userId)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var spec = new NotificationSpec(userId, notificationId);
        var userNotification = await UserNotifications.GetByIdAsync(spec);

        if (userNotification is null) throw new NotificationNotFoundException($"Notification with ID {notificationId} not found for user {userId}.");

        UserNotifications.Delete(userNotification);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task SendAsync(int userId, NotificationType type, string message, string? title = null, string? clickUrl = null, string? imageUrl = null)
    {
        var notification = new Notification
        {
            Message = message,
            Type = type,
            Title = title,
            ClickUrl = clickUrl,
            ImageUrl = imageUrl,
            CreatedAt = EgyptTime.Now
        };

        Notifications.Add(notification);
        await _unitOfWork.SaveChangesAsync();

        var settingsRepo = _unitOfWork.GetRepository<UserNotificationSettings, int>();
        var allSettings = await settingsRepo.GetAllAsync();
        var setting = allSettings.FirstOrDefault(s => s.UserId == userId);
        var inAppEnabled = setting?.InAppNotificationsEnabled ?? true;
        var pushEnabled = setting?.PushNotificationsEnabled ?? false;

        if (inAppEnabled)
        {
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

        if (pushEnabled)
            await DispatchPushAsync(notification, [userId]);
    }

    public async Task SendToManyAsync(
        IEnumerable<int> userIds,
        NotificationType type,
        string message,
        string? title = null,
        string? clickUrl = null,
        string? imageUrl = null)
    {
        var userIdList = userIds.ToList();
        if (userIdList.Count == 0) return;

        var settingsRepo = _unitOfWork.GetRepository<UserNotificationSettings, int>();
        var allSettings = await settingsRepo.GetAllAsync();
        var usersWithInAppEnabled = allSettings
            .Where(s => userIdList.Contains(s.UserId))
            .ToDictionary(s => s.UserId, s => s.InAppNotificationsEnabled);
        var usersWithPushEnabled = allSettings
            .Where(s => userIdList.Contains(s.UserId) && s.PushNotificationsEnabled)
            .Select(s => s.UserId)
            .ToHashSet();

        var notification = new Notification
        {
            Message = message,
            Type = type,
            Title = title,
            ClickUrl = clickUrl,
            ImageUrl = imageUrl,
            CreatedAt = EgyptTime.Now
        };

        Notifications.Add(notification);
        await _unitOfWork.SaveChangesAsync();

        var createdUserNotifications = new List<UserNotification>();

        foreach (var userId in userIdList)
        {
            var inAppEnabled = usersWithInAppEnabled.GetValueOrDefault(userId, true);
            if (!inAppEnabled) continue;

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

        if (usersWithPushEnabled.Count > 0)
            await DispatchPushAsync(notification, usersWithPushEnabled);
    }

    private static NotificationDto MapToDto(UserNotification un) => new()
    {
        NotificationId = un.NotificationId,
        UserNotificationId = un.UserNotificationId,
        Message = un.Notification.Message,
        Type = un.Notification.Type,
        TypeLabel = GetTypeLabel(un.Notification.Type),
        Title = un.Notification.Title,
        ClickUrl = un.Notification.ClickUrl,
        ImageUrl = un.Notification.ImageUrl,
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
        NotificationType.QuestionRouting => "Question Routing",
        NotificationType.ElectiveBucketLocked => "Elective Bucket Locked",
        NotificationType.NewMessage => "New Message",
        NotificationType.NewComment => "New Comment",
        NotificationType.NewUpvote => "New Upvote",
        NotificationType.FriendRequestReceived => "Friend Request",
        _ => "Notification"
    };

    private static string GetTimeAgo(DateTime createdAt)
    {
        var diff = EgyptTime.Now - createdAt;

        return diff.TotalMinutes < 1 ? "Just now"
             : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes} minutes ago"
             : diff.TotalHours < 24 ? $"{(int)diff.TotalHours} hours ago"
             : diff.TotalDays < 7 ? $"{(int)diff.TotalDays} days ago"
             : diff.TotalDays < 30 ? $"{(int)(diff.TotalDays / 7)} weeks ago"
             : diff.TotalDays < 365 ? $"{(int)(diff.TotalDays / 30)} months ago"
              : $"{(int)(diff.TotalDays / 365)} years ago";
    }

    private async Task DispatchPushAsync(Notification notification, IEnumerable<int> userIds)
    {
        try
        {
            var deviceTokenRepo = _unitOfWork.GetRepository<DeviceToken, int>();
            var spec = new DeviceTokenSpec(userIds);
            var activeTokens = (await deviceTokenRepo.GetAllAsync(spec, asNoTracking: true)).ToList();

            if (activeTokens.Count == 0) return;

            var result = await _pushSender.SendAsync(
                activeTokens,
                notification.Title,
                notification.Message,
                notification.ClickUrl,
                notification.ImageUrl,
                notification.NotificationId);

            if (result.InvalidTokens.Count > 0)
            {
                foreach (var invalidToken in result.InvalidTokens)
                {
                    invalidToken.IsActive = false;
                    deviceTokenRepo.Update(invalidToken);
                }
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch
        {
            // Push delivery failure must never break in-app notification delivery
        }
    }
}