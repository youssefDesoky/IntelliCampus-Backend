namespace IntelliCampus.shared.Dtos.Attendance;

public class QrTokenDto
{
    public string QrPayload { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int Iteration { get; set; }
    public bool IsFinal { get; set; }
}
