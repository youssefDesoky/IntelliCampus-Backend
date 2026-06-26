namespace IntelliCampus.Shared.Dtos.Auth;

public class ChangeRecoveryEmailDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewEmail { get; set; } = null!;
}
