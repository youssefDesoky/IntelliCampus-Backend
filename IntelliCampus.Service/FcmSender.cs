using FirebaseAdmin.Messaging;
using IntelliCampus.Service_Abstraction;

namespace IntelliCampus.Service;

public class FcmSender : IFcmSender
{
    public async Task<FcmSendResult> SendAsync(IEnumerable<string> tokens, string? title, string body, string? clickUrl, string? imageUrl, int? notificationId)
    {
        var tokenList = tokens.ToList();
        if (tokenList.Count == 0) return new FcmSendResult();

        var message = new MulticastMessage
        {
            Tokens = tokenList,
            Notification = new Notification
            {
                Title = title ?? "IntelliCampus",
                Body = body,
                ImageUrl = imageUrl,
            },
            Data = new Dictionary<string, string>
            {
                ["clickUrl"] = clickUrl ?? "/",
                ["notificationId"] = notificationId?.ToString() ?? ""
            },
            Webpush = new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Icon = "/images/IntelliCampusLogo.png",
                    Badge = "/IntelliCampus_Trans.ico"
                }
            }
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

        var invalidTokens = new List<string>();
        for (int i = 0; i < response.Responses.Count; i++)
        {
            if (!response.Responses[i].IsSuccess &&
                (response.Responses[i].Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                 response.Responses[i].Exception?.MessagingErrorCode == MessagingErrorCode.InvalidArgument))
            {
                invalidTokens.Add(tokenList[i]);
            }
        }

        return new FcmSendResult
        {
            SuccessCount = response.SuccessCount,
            FailureCount = response.FailureCount,
            InvalidTokens = invalidTokens
        };
    }
}
