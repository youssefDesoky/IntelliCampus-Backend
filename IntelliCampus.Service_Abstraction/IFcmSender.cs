namespace IntelliCampus.Service_Abstraction;

public class FcmSendResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> InvalidTokens { get; set; } = [];
}

public interface IFcmSender
{
    Task<FcmSendResult> SendAsync(IEnumerable<string> tokens, string? title, string body, string? clickUrl, string? imageUrl, int? notificationId);
}
