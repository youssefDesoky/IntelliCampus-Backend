namespace IntelliCampus.Shared.Dtos.Auth;

public class ChangeRecoveryEmailDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewEmail { get; set; } = null!;
    public string? VerificationCode { get; set; }
}

public class SendChangeRecoveryEmailCodeDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewEmail { get; set; } = null!;
}
