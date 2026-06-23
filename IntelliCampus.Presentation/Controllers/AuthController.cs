using System.Security.Claims;
using IntelliCampus.Shared.Dtos.Auth;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IAuthService authService, IOptions<JwtSettings> jwtSettings)
    {
        _authService = authService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        // Set token in HttpOnly cookie
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

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
