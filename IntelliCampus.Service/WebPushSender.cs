using System.Text.Json;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using Microsoft.Extensions.Options;
using WebPush;

namespace IntelliCampus.Service;

public class WebPushSender : IPushSender
{
    private readonly VapidSettings _vapidSettings;

    public WebPushSender(IOptions<VapidSettings> vapidSettings)
    {
        _vapidSettings = vapidSettings.Value;
    }

    public async Task<PushSendResult> SendAsync(IEnumerable<DeviceToken> subscriptions, string? title, string body, string? clickUrl, string? imageUrl, int? notificationId)
    {
        var subList = subscriptions.ToList();
        if (subList.Count == 0) return new PushSendResult();

        var pushClient = new WebPushClient();
        var vapidDetails = new VapidDetails(_vapidSettings.Subject, _vapidSettings.PublicKey, _vapidSettings.PrivateKey);

        var payload = JsonSerializer.Serialize(new
        {
            title = title ?? "IntelliCampus",
            body,
            clickUrl = clickUrl ?? "/",
            imageUrl,
            notificationId
        });

        var successCount = 0;
        var failureCount = 0;
        var invalidTokens = new List<DeviceToken>();

        foreach (var sub in subList)
        {
            var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
            try
            {
                await SendOneAsync(pushClient, pushSubscription, payload, vapidDetails);
                successCount++;
            }
            catch (Exception ex)
            {
                // 410 Gone / 404 Not Found = subscription expired or invalid
                if (ex.Message.Contains("410") || ex.Message.Contains("404") ||
                    ex.Message.Contains("Gone", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
                {
                    invalidTokens.Add(sub);
                }
                failureCount++;
            }
        }

        return new PushSendResult
        {
            SuccessCount = successCount,
            FailureCount = failureCount,
            InvalidTokens = invalidTokens
        };
    }

    protected virtual Task SendOneAsync(WebPushClient client, PushSubscription subscription, string payload, VapidDetails vapidDetails)
    {
        return client.SendNotificationAsync(subscription, payload, new Dictionary<string, object?>
        {
            ["vapidDetails"] = vapidDetails
        });
    }
}
