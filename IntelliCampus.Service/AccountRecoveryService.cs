using System.Security.Cryptography;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Auth;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class AccountRecoveryService : IAccountRecoveryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountRecoveryService> _logger;

    public AccountRecoveryService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ITokenService tokenService,
        IEmailSender emailSender,
        ILogger<AccountRecoveryService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _logger = logger;
    }

    private IGenericRepository<User, int> Users => _unitOfWork.GetRepository<User, int>();
    private IGenericRepository<EmailVerificationCode, long> VerificationCodes => _unitOfWork.GetRepository<EmailVerificationCode, long>();
    private IGenericRepository<PasswordResetToken, long> ResetTokens => _unitOfWork.GetRepository<PasswordResetToken, long>();

    public async Task SendVerificationCodeAsync(int userId, string recoveryEmail)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = HashCode(code);

        var entity = new EmailVerificationCode
        {
            UserId = userId,
            Email = recoveryEmail,
            CodeHash = codeHash,
            Purpose = "recovery_email_setup",
            ExpiresAt = EgyptTime.Now.AddMinutes(10),
            CreatedAt = EgyptTime.Now
        };

        VerificationCodes.Add(entity);
        await _unitOfWork.SaveChangesAsync();

        await _emailSender.SendAsync(
            recoveryEmail,
            user.FullName,
            "Email Verification Code",
            $"""
            <h2 style="color:#1a56db;text-align:center;margin:0 0 24px;">Email Verification</h2>
            <p>Hello <strong>{user.FullName}</strong>,</p>
            <p>Your verification code is:</p>
            <div style="text-align:center;margin:24px 0;">
                <span style="font-size:32px;font-weight:bold;letter-spacing:8px;background:#f3f4f6;padding:12px 24px;border-radius:8px;">{code}</span>
            </div>
            <p>This code expires in <strong>10 minutes</strong>.</p>
            <p style="color:#6b7280;font-size:13px;">If you did not request this, please ignore this email.</p>
            """);
    }

    public async Task<AuthResponseDto> FirstTimeSetupAsync(int userId, FirstTimeSetupDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        if (dto.NewPassword.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters.");

        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        var recentCodes = (await VerificationCodes.GetAllAsync())
            .Where(c => c.UserId == userId
                && c.Email == dto.RecoveryEmail
                && c.Purpose == "recovery_email_setup"
                && c.ConsumedAt == null
                && c.ExpiresAt > EgyptTime.Now)
            .ToList();

        var matchedCode = recentCodes.FirstOrDefault(c =>
            HashCode(dto.VerificationCode) == c.CodeHash);

        if (matchedCode is null)
            throw new InvalidOperationException("Invalid or expired verification code.");

        matchedCode.ConsumedAt = EgyptTime.Now;
        VerificationCodes.Update(matchedCode);

        user.Password = _passwordService.HashPassword(dto.NewPassword);
        user.RecoveryEmail = dto.RecoveryEmail;
        user.RecoveryEmailVerified = true;
        user.MustChangePassword = false;
        Users.Update(user);

        await _unitOfWork.SaveChangesAsync();

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, string? ipAddress, string? userAgent)
    {
        var users = await Users.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == dto.Email);

        if (user is null || string.IsNullOrWhiteSpace(user.RecoveryEmail) || !user.RecoveryEmailVerified)
        {
            _logger.LogInformation("Forgot-password request for {Email}: no user or no verified recovery email", dto.Email);
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashCode(rawToken);

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            ExpiresAt = EgyptTime.Now.AddMinutes(30),
            CreatedAt = EgyptTime.Now
        };

        ResetTokens.Add(resetToken);
        await _unitOfWork.SaveChangesAsync();

        var resetUrl = $"{GetFrontendUrl()}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        await _emailSender.SendAsync(
            user.RecoveryEmail,
            user.FullName,
            "Password Reset Request",
            $"""
            <h2 style="color:#1a56db;text-align:center;margin:0 0 24px;">Password Reset</h2>
            <p>Hello <strong>{user.FullName}</strong>,</p>
            <p>Click the button below to reset your password. This link expires in <strong>30 minutes</strong>.</p>
            <div style="text-align:center;margin:24px 0;">
                <a href="{resetUrl}" style="display:inline-block;background:#1a56db;color:#fff;padding:12px 32px;border-radius:6px;text-decoration:none;font-weight:bold;">Reset Password</a>
            </div>
            <p style="color:#6b7280;font-size:13px;">If you did not request this, please ignore this email.</p>
            """);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        if (dto.NewPassword.Length < 6)
            throw new InvalidOperationException("Password must be at least 6 characters.");

        var activeTokens = await ResetTokens.GetAllAsync();
        var hashedInput = HashCode(dto.Token);
        var resetToken = activeTokens.FirstOrDefault(t =>
            t.ConsumedAt == null && t.ExpiresAt > EgyptTime.Now && t.TokenHash == hashedInput);

        if (resetToken is null)
            throw new InvalidOperationException("Invalid or expired reset token.");

        resetToken.ConsumedAt = EgyptTime.Now;
        ResetTokens.Update(resetToken);

        var user = await Users.GetByIdAsync(resetToken.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        user.Password = _passwordService.HashPassword(dto.NewPassword);
        user.MustChangePassword = false;
        Users.Update(user);

        await _unitOfWork.SaveChangesAsync();
    }

    private static string HashCode(string input)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    private static string GetFrontendUrl()
    {
        var envUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        return !string.IsNullOrWhiteSpace(envUrl) ? envUrl : "https://localhost:5173";
    }
}
