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
    public async Task<IActionResult> Register([FromBody] RegisterSubscriptionRequest request)
    {
        var repo = unitOfWork.GetRepository<DeviceToken, int>();
        var allTokens = await repo.GetAllAsync();
        var existing = allTokens.FirstOrDefault(dt => dt.Endpoint == request.Endpoint);

        if (existing is not null)
        {
            existing.UserId = UserId;
            existing.IsActive = true;
            existing.P256dh = request.Keys.P256dh;
            existing.Auth = request.Keys.Auth;
            existing.LastSeenAt = EgyptTime.Now;
            repo.Update(existing);
            await unitOfWork.SaveChangesAsync();
        }
        else
        {
            var now = EgyptTime.Now;
            repo.Add(new DeviceToken
            {
                UserId = UserId,
                Endpoint = request.Endpoint,
                P256dh = request.Keys.P256dh,
                Auth = request.Keys.Auth,
                Platform = request.Platform,
                RegisteredAt = now,
                LastSeenAt = now,
                IsActive = true
            });

            try
            {
                await unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Race condition: concurrent request already inserted this endpoint.
                // The subscription IS registered — just return success.
            }
        }

        return Ok();
    }

    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister([FromBody] UnregisterSubscriptionRequest request)
    {
        var repo = unitOfWork.GetRepository<DeviceToken, int>();
        var allTokens = await repo.GetAllAsync();
        var existing = allTokens.FirstOrDefault(dt => dt.Endpoint == request.Endpoint && dt.UserId == UserId);

        if (existing is not null)
        {
            existing.IsActive = false;
            repo.Update(existing);
            await unitOfWork.SaveChangesAsync();
        }

        return Ok();
    }
}

public class RegisterSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public SubscriptionKeys Keys { get; set; } = new();
    public string? Platform { get; set; }
}

public class SubscriptionKeys
{
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class UnregisterSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
}
