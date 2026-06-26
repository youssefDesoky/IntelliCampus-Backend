namespace IntelliCampus.Shared.Dtos.Auth;

public class ForgotPasswordDto
{
    public string Email { get; set; } = null!;
    public string? TurnstileToken { get; set; }
}
