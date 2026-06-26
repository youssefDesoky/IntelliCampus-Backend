using IntelliCampus.Domain.Helpers;

namespace IntelliCampus.Domain.Entities;

public class SecurityAuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Purpose { get; set; } = null!;
    public string? NationalIdMasked { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime AttemptedAt { get; set; } = EgyptTime.Now;

    public User? User { get; set; }
}
