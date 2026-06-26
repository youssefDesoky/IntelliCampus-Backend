namespace IntelliCampus.Shared.Dtos.Auth;

public class FirstTimeSetupDto
{
    public string RecoveryEmail { get; set; } = null!;
    public string VerificationCode { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class SendVerificationCodeDto
{
    public string RecoveryEmail { get; set; } = null!;
}
