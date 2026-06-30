using IntelliCampus.Shared.Dtos.Auth;

namespace IntelliCampus.Service_Abstraction;

public interface IAccountRecoveryService
{
    Task SendVerificationCodeAsync(int userId, string recoveryEmail);
    Task SendChangeRecoveryEmailCodeAsync(int userId, SendChangeRecoveryEmailCodeDto dto);
    Task ChangeRecoveryEmailAsync(int userId, ChangeRecoveryEmailDto dto);
    Task<AuthResponseDto> FirstTimeSetupAsync(int userId, FirstTimeSetupDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto, string? ipAddress, string? userAgent);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}
