namespace IntelliCampus.shared.Dtos.Attendance;

public class QrPayload
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Token { get; set; } = string.Empty;
}
