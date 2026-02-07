using System.Security.Claims;
using IntelliCampus.BLL.Dtos.Auth;
using IntelliCampus.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (result is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponseDto>> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _authService.GetMeAsync(userId.Value);

        if (result is null)
            return NotFound();

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

        if (profile is null)
            return NotFound();

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

        if (profile is null)
            return NotFound();

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("profile/image")]
    public async Task<IActionResult> UpdateProfileImage([FromBody] string imageUrl)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _authService.UpdateProfileImageAsync(userId.Value, imageUrl);

        if (result is null)
            return NotFound();

        return Ok(new { profileImage = result });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _authService.ChangePasswordAsync(userId.Value, dto);

        if (!result)
            return BadRequest(new { message = "Current password is incorrect." });

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
