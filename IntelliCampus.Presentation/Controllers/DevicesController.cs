using System.Security.Claims;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController(IUnitOfWork unitOfWork) : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTokenRequest request)
    {
        var repo = unitOfWork.GetRepository<DeviceToken, int>();
        var allTokens = await repo.GetAllAsync();
        var existing = allTokens.FirstOrDefault(dt => dt.Token == request.Token);

        if (existing is not null)
        {
            existing.UserId = UserId;
            existing.IsActive = true;
            existing.LastSeenAt = DateTime.UtcNow;
            repo.Update(existing);
        }
        else
        {
            repo.Add(new DeviceToken
            {
                UserId = UserId,
                Token = request.Token,
                Platform = request.Platform,
                RegisteredAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await unitOfWork.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister([FromBody] RegisterTokenRequest request)
    {
        var repo = unitOfWork.GetRepository<DeviceToken, int>();
        var allTokens = await repo.GetAllAsync();
        var existing = allTokens.FirstOrDefault(dt => dt.Token == request.Token && dt.UserId == UserId);

        if (existing is not null)
        {
            existing.IsActive = false;
            repo.Update(existing);
            await unitOfWork.SaveChangesAsync();
        }

        return Ok();
    }
}

public class RegisterTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string? Platform { get; set; }
}
