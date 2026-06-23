namespace IntelliCampus.Domain.Entities;

public class DeviceToken
{
    public int DeviceTokenId { get; set; }
    public int UserId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;
    public string? Platform { get; set; }
    public DateTime RegisteredAt { get; set; } = EgyptTime.Now;
    public DateTime LastSeenAt { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
}
