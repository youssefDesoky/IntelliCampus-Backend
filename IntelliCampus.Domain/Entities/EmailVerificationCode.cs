using IntelliCampus.Domain.Helpers;

namespace IntelliCampus.Domain.Entities;

public class EmailVerificationCode
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string CodeHash { get; set; } = null!;
    public string Purpose { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; } = EgyptTime.Now;

    public User User { get; set; } = null!;
}
