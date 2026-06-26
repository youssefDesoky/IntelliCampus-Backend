using IntelliCampus.Domain.Helpers;

namespace IntelliCampus.Domain.Entities;

public class PasswordResetToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; } = EgyptTime.Now;

    public User User { get; set; } = null!;
}
