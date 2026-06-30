using System.Security.Claims;
using IntelliCampus.Shared.Dtos.Auth;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICredentialRetrievalService _credentialRetrievalService;
    private readonly IAccountRecoveryService _accountRecoveryService;
    private readonly ITurnstileVerifier _turnstileVerifier;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        IAuthService authService,
        ICredentialRetrievalService credentialRetrievalService,
        IAccountRecoveryService accountRecoveryService,
        ITurnstileVerifier turnstileVerifier,
        IOptions<JwtSettings> jwtSettings)
    {
        _authService = authService;
        _credentialRetrievalService = credentialRetrievalService;
        _accountRecoveryService = accountRecoveryService;
        _turnstileVerifier = turnstileVerifier;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        Response.Cookies.Append("token", result.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = result.ExpiresAt
        });

        return Ok(new LoginResponseDto
        {
            UserId = result.UserId,
            Email = result.Email,
            FullName = result.FullName,
            Roles = result.Roles
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("token", new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax
        });

        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponseDto>> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _authService.GetMeAsync(userId.Value);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var profile = await _authService.GetProfileAsync(userId.Value);

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var profile = await _authService.UpdateProfileAsync(userId.Value, dto);

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("profile/image")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfileImage(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var profile = await _authService.UpdateProfileImageAsync(userId.Value, file);

        return Ok(profile);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        await _authService.ChangePasswordAsync(userId.Value, dto);

        return Ok(new { message = "Password changed successfully." });
    }

    [Authorize]
    [HttpPost("change-recovery-email/send-code")]
    public async Task<IActionResult> SendChangeRecoveryEmailCode(SendChangeRecoveryEmailCodeDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        await _accountRecoveryService.SendChangeRecoveryEmailCodeAsync(userId.Value, dto);

        return Ok(new { message = "Verification code sent to your new recovery email." });
    }

    [Authorize]
    [HttpPost("change-recovery-email")]
    public async Task<IActionResult> ChangeRecoveryEmail(ChangeRecoveryEmailDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        await _accountRecoveryService.ChangeRecoveryEmailAsync(userId.Value, dto);

        return Ok(new { message = "Recovery email changed successfully." });
    }

    [AllowAnonymous]
    [HttpPost("get-credentials")]
    [EnableRateLimiting("GetCredentials")]
    public async Task<ActionResult<GetCredentialsResponseDto>> GetCredentials([FromBody] GetCredentialsDto dto)
    {
        if (!await _turnstileVerifier.VerifyAsync(dto.TurnstileToken))
            return BadRequest(new { message = "Verification failed. Please try again." });

        var result = await _credentialRetrievalService.GetCredentialsAsync(
            dto, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!await _turnstileVerifier.VerifyAsync(dto.TurnstileToken))
            return BadRequest(new { message = "Verification failed. Please try again." });

        await _accountRecoveryService.ForgotPasswordAsync(
            dto, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

        return Ok(new { message = "If the email exists, a reset link has been sent to your recovery email." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        await _accountRecoveryService.ResetPasswordAsync(dto);
        return Ok(new { message = "Password has been reset successfully. You can now login." });
    }

    [Authorize]
    [HttpPost("first-time-setup/send-code")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        await _accountRecoveryService.SendVerificationCodeAsync(userId.Value, dto.RecoveryEmail);
        return Ok(new { message = "Verification code sent to your email." });
    }

    [Authorize]
    [HttpPost("first-time-setup")]
    public async Task<ActionResult<LoginResponseDto>> FirstTimeSetup([FromBody] FirstTimeSetupDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _accountRecoveryService.FirstTimeSetupAsync(userId.Value, dto);

        Response.Cookies.Append("token", result.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = result.ExpiresAt
        });

        return Ok(new LoginResponseDto
        {
            UserId = result.UserId,
            Email = result.Email,
            FullName = result.FullName,
            Roles = result.Roles
        });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
