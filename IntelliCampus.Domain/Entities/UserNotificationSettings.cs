namespace IntelliCampus.Domain.Entities;

public class UserNotificationSettings
{
    public int UserNotificationSettingsId { get; set; }
    public int UserId { get; set; }
    public bool InAppNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = false;

    public User User { get; set; } = null!;
}
