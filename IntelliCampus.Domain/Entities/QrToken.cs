namespace IntelliCampus.Domain.Entities;

public class QrToken
{
    public int QrTokenId { get; set; }
    public int StudentId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool IsExpired => EgyptTime.Now > ExpiresAt;

    public Student Student { get; set; } = null!;
}
