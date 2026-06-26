namespace IntelliCampus.shared.Dtos.Attendance;

public class QrPayload
{
    public int UserId { get; set; }
    public long Timestamp { get; set; }
    public string Token { get; set; } = string.Empty;
}
