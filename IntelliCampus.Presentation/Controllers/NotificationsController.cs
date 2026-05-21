using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(INotificationService notificationService)
    : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await notificationService.GetByUserIdAsync(UserId));

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
        => Ok(await notificationService.GetUnreadAsync(UserId));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(await notificationService.GetSummaryAsync(UserId));

    [HttpGet("unread/count")]
    public async Task<IActionResult> GetUnreadCount()
        => Ok(new { count = await notificationService.GetUnreadCountAsync(UserId) });

    [HttpPut("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var result = await notificationService.MarkAsReadAsync(notificationId, UserId);
        return result ? Ok() : NotFound();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await notificationService.MarkAllAsReadAsync(UserId);
        return Ok();
    }

    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> Delete(int notificationId)
    {
        var result = await notificationService.DeleteAsync(notificationId, UserId);
        return result ? Ok() : NotFound();
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin,Instructor")]
    public async Task<IActionResult> SendBulk(SendBulkNotificationDto dto)
    {
        if (dto.UserIds is null || dto.UserIds.Count == 0)
            return BadRequest("At least one user must be specified.");

        await notificationService.SendToManyAsync(dto.UserIds, dto.Type, dto.Message);

        return Ok(new { sent = dto.UserIds.Count });
    }
}
