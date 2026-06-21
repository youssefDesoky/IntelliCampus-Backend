using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service_Abstraction;

public class PushSendResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<DeviceToken> InvalidTokens { get; set; } = [];
}

public interface IPushSender
{
    Task<PushSendResult> SendAsync(IEnumerable<DeviceToken> subscriptions, string? title, string body, string? clickUrl, string? imageUrl, int? notificationId);
}
