namespace IntelliCampus.Shared.Dtos.Auth;

public class ResetPasswordDto
{
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}
