using System.ComponentModel.DataAnnotations;

namespace IntelliCampus.Shared.Dtos.Auth;

public class FirstTimeSetupDto
{
    [Required(ErrorMessage = "Recovery email is required.")]
    [EmailAddress(ErrorMessage = "Recovery email is not valid.")]
    public string RecoveryEmail { get; set; } = null!;

    [Required(ErrorMessage = "Verification code is required.")]
    public string VerificationCode { get; set; } = null!;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = null!;
}

public class SendVerificationCodeDto
{
    [Required(ErrorMessage = "Recovery email is required.")]
    [EmailAddress(ErrorMessage = "Recovery email is not valid.")]
    public string RecoveryEmail { get; set; } = null!;
}
